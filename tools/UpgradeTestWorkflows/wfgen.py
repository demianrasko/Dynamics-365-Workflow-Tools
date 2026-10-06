"""Builds classic workflow XAML the way the workflow designer writes it.

The patterns are copied from workflows built in the designer, and the generated steps match the designer's output
character for character. Each custom step lists every input and output of the activity, read from the activity's
registration (plugintype.customworkflowactivityinfo), so a step can't name an argument the activity doesn't have.
"""
import re
import xml.etree.ElementTree

WFA = 'Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
CAST = {'x:String': 'System.String', 'x:Int32': 'System.Int32', 'x:Boolean': 'System.Boolean', 'x:Decimal': 'System.Decimal',
        'x:Double': 'System.Double', 's:DateTime': 'System.DateTime', 'mxs:EntityReference': 'Microsoft.Xrm.Sdk.EntityReference'}
VARIABLE_DEFAULT = {'x:String': '[Nothing]', 'x:Int32': '0', 'x:Boolean': 'False', 'x:Decimal': '0', 'x:Double': '0', 's:DateTime': '',
                    'mxs:EntityReference': '[New EntityReference()]'}
PROPERTY_TYPE = {'x:String': 'String', 'x:Int32': 'Integer', 'x:Boolean': 'Boolean', 'x:Decimal': 'Decimal', 'x:Double': 'Float'}
REGISTERED_TYPE = {'System.String': 'x:String', 'Microsoft.Crm.Sdk.Lookup': 'mxs:EntityReference', 'Microsoft.Crm.Sdk.CrmBoolean': 'x:Boolean',
                   'Microsoft.Crm.Sdk.CrmNumber': 'x:Int32', 'Microsoft.Crm.Sdk.CrmDecimal': 'x:Decimal', 'Microsoft.Crm.Sdk.CrmFloat': 'x:Double',
                   'Microsoft.Crm.Sdk.CrmDateTime': 's:DateTime'}
# the table behind lookup outputs that don't name one, for the record the designer retrieves after the step
OUTPUT_ENTITY = {'InitiatingUser': 'systemuser', 'DefaultTeam': 'team', 'createdTeam': 'team'}
HEADER = ('<?xml version="1.0" encoding="utf-16"?><Activity x:Class="{cls}" xmlns="http://schemas.microsoft.com/netfx/2009/xaml/activities" '
          'xmlns:mva="clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" '
          'xmlns:mxs="clr-namespace:Microsoft.Xrm.Sdk;assembly=Microsoft.Xrm.Sdk, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" '
          'xmlns:mxswa="clr-namespace:Microsoft.Xrm.Sdk.Workflow.Activities;assembly=Microsoft.Xrm.Sdk.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" '
          'xmlns:s="clr-namespace:System;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" '
          'xmlns:scg="clr-namespace:System.Collections.Generic;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" '
          'xmlns:sco="clr-namespace:System.Collections.ObjectModel;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" '
          'xmlns:srs="clr-namespace:System.Runtime.Serialization;assembly=System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" '
          'xmlns:this="clr-namespace:" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">'
          '<x:Members><x:Property Name="InputEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" />'
          '<x:Property Name="CreatedEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /></x:Members>'
          '<this:{cls}.InputEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:{cls}.InputEntities>'
          '<this:{cls}.CreatedEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:{cls}.CreatedEntities>'
          '<mva:VisualBasic.Settings>Assembly references and imported namespaces for internal implementation</mva:VisualBasic.Settings>')
PRIMARY = 'InputEntities(&quot;primaryEntity&quot;)'

ACTIVITIES = {}


def load_activities(registrations):
    """registrations: [{'typename': ..., 'info': customworkflowactivityinfo}] from the installed assembly."""
    def parameters(block):
        found = []
        for p in re.findall(r'<CustomActivityParameterInfo>(.*?)</CustomActivityParameterInfo>', block or '', re.S):
            default = re.search(r'<DefaultValue[^>]*>([^<]*)</DefaultValue>', p)
            found.append(dict(prop=re.search(r'<DependencyPropertyName>([^<]+)', p).group(1),
                              type=REGISTERED_TYPE[re.search(r'<TypeName>([^,<]+)', p).group(1)],
                              default=default.group(1) if default else None,
                              entities=[e for e in re.findall(r'<string>([^<]*)</string>', p) if e]))
        return found

    for r in registrations:
        info = r['info']
        inputs = re.search(r'<Inputs>(.*?)</Inputs>', info, re.S)
        outputs = re.search(r'<Outputs>(.*?)</Outputs>', info, re.S)
        ACTIVITIES[r['typename']] = dict(aqn=re.search(r'<AssemblyQualifiedName>([^<]+)', info).group(1),
                                         inputs=parameters(inputs and inputs.group(1)), outputs=parameters(outputs and outputs.group(1)))


def encode(value):
    """A text value as the designer stores it: VB quotes doubled, HTML-encoded, then XML-escaped."""
    value = value.replace('"', '""')
    value = (value.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;').replace('"', '&#34;')
             .replace('\r', '&#13;').replace('\n', '&#10;'))
    return value.replace('&', '&amp;')


def type_literal(t):
    return f'<mxswa:ReferenceLiteral x:TypeArguments="s:Type" Value="{t}" />'


def expression(operator, parameters, target, result):
    return (f'<mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.EvaluateExpression, {WFA}" DisplayName="EvaluateExpression">'
            '<mxswa:ActivityReference.Arguments>'
            f'<InArgument x:TypeArguments="x:String" x:Key="ExpressionOperator">{operator}</InArgument>'
            f'<InArgument x:TypeArguments="s:Object[]" x:Key="Parameters">[New Object() {{ {parameters} }}]</InArgument>'
            f'<InArgument x:TypeArguments="s:Type" x:Key="TargetType">{type_literal(target)}</InArgument>'
            f'<OutArgument x:TypeArguments="x:Object" x:Key="Result">[{result}]</OutArgument>'
            '</mxswa:ActivityReference.Arguments></mxswa:ActivityReference>')


def convert(variable, target):
    return (f'<mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.ConvertCrmXrmTypes, {WFA}" DisplayName="ConvertCrmXrmTypes">'
            '<mxswa:ActivityReference.Arguments>'
            f'<InArgument x:TypeArguments="x:Object" x:Key="Value">[{variable}]</InArgument>'
            f'<InArgument x:TypeArguments="s:Type" x:Key="TargetType">{type_literal(target)}</InArgument>'
            f'<OutArgument x:TypeArguments="x:Object" x:Key="Result">[{variable}_converted]</OutArgument>'
            '</mxswa:ActivityReference.Arguments></mxswa:ActivityReference>')


def get_property(attribute, entity, entity_name, result, target):
    return (f'<mxswa:GetEntityProperty Attribute="{attribute}" Entity="[{entity}]" EntityName="{entity_name}" Value="[{result}]">'
            f'<mxswa:GetEntityProperty.TargetType><InArgument x:TypeArguments="s:Type">{type_literal(target)}</InArgument></mxswa:GetEntityProperty.TargetType>'
            '</mxswa:GetEntityProperty>')


def created(key):
    return f'CreatedEntities(&quot;{key}&quot;)'


# ---- values: each returns a function (builder, result variable, target type) -> xaml -------------------------------

def Text(value):
    return lambda b, variable, t: expression('CreateCrmType', f'Microsoft.Xrm.Sdk.Workflow.WorkflowPropertyType.String, "{encode(value)}", "String"', 'x:String', variable)


def Number(value):
    return lambda b, variable, t: expression('CreateCrmType', f'Microsoft.Xrm.Sdk.Workflow.WorkflowPropertyType.{PROPERTY_TYPE[t]}, "{value}"', t, variable)


def Yes(value=True):
    return lambda b, variable, t: expression('CreateCrmType', f'Microsoft.Xrm.Sdk.Workflow.WorkflowPropertyType.Boolean, "{value}"', 'x:Boolean', variable)


def No():
    return Yes(False)


def Lookup(record):
    """A fixed record: {'entity': ..., 'id': ..., 'name': ...}."""
    def value(b, variable, t):
        key = b.temp()
        return (expression('CreateCrmType', f'Microsoft.Xrm.Sdk.Workflow.WorkflowPropertyType.Guid, "{record["id"]}", "Key"', 'mxs:EntityReference', key)
                + expression('CreateCrmType', f'Microsoft.Xrm.Sdk.Workflow.WorkflowPropertyType.EntityReference, "{record["entity"]}", "{encode(record["name"])}", {key}, "Lookup"',
                             'mxs:EntityReference', variable))
    return value


def Column(attribute, target=None):
    """A column of the workflow's record."""
    def value(b, variable, t):
        holder = b.temp()
        return get_property(attribute, PRIMARY, b.primary, holder, target or t) + expression('SelectFirstNonNull', holder, target or t, variable)
    return value


# Record URL (Dynamic)
RecordUrl = Column('!Process_Custom_Attribute_URL_', 'x:String')


def ThisRecord():
    return lambda b, variable, t: Column(f'{b.primary}id', 'mxs:EntityReference')(b, variable, t)


def Output(step, prop):
    """An output of an earlier custom step, by step number."""
    return lambda b, variable, t: expression('SelectFirstNonNull', f'CustomActivityStep{step}{prop}_localParameter', t, variable)


def OutputColumn(step, prop, entity, attribute):
    """A column of the record behind a lookup output, e.g. the initiating user's full name."""
    def value(b, variable, t):
        holder = b.temp()
        return (get_property(attribute, created(f'CustomActivityStep{step}{prop}_entity'), entity, holder, 'x:String')
                + expression('SelectFirstNonNull', holder, 'x:String', variable))
    return value


def CreatedRecord(step, entity):
    """The record a Create Record step made, by step number."""
    def value(b, variable, t):
        holder = b.temp()
        key = 'activityid' if entity == 'email' else f'{entity}id'
        return (get_property(key, created(f'CreateStep{step}_localParameter'), entity, holder, 'mxs:EntityReference')
                + expression('SelectFirstNonNull', holder, 'mxs:EntityReference', variable))
    return value


def Join(*parts):
    """Text and values joined together."""
    def value(b, variable, t):
        names, xaml = [], ''
        for part in parts:
            name = b.temp()
            names.append(name)
            xaml += (Text(part) if isinstance(part, str) else part)(b, name, 'x:String')
        return xaml + expression('Add', ', '.join(names), 'x:String', variable)
    return value


class Builder:
    def __init__(self, primary):
        self.primary = primary
        self.step = 0
        self.outputs = []
        self.variables = []
        self.prefix = None
        self.count = 0

    def temp(self):
        self.count += 1
        name = f'{self.prefix}_{self.count}'
        self.variables.append(name)
        return name

    def _start(self, kind):
        self.step += 1
        self.prefix, self.count, self.variables = f'{kind}{self.step}', 0, []
        return self.step

    def custom(self, typename, label, inputs=None):
        """A custom activity step. Inputs left out get the activity's default, as in the designer."""
        inputs = inputs or {}
        activity = ACTIVITIES[typename]
        unknown = set(inputs) - {p['prop'] for p in activity['inputs']}
        if unknown:
            raise ValueError(f'{typename} has no input {", ".join(sorted(unknown))}')
        n = self._start('CustomActivityStep')
        body, arguments, after = '', '', ''
        for p in activity['inputs']:
            variable = self.temp()
            value = inputs.get(p['prop'])
            if value is None and p['default'] is not None and p['type'] != 'mxs:EntityReference':
                d = p['default']
                value = Yes(d.capitalize()) if p['type'] == 'x:Boolean' else Text(d) if p['type'] == 'x:String' else Number(d)
            if value is not None:
                body += value(self, variable, p['type'])
            self.variables.append(f'{variable}_converted')
            body += convert(variable, p['type'])
            arguments += f'<InArgument x:TypeArguments="{p["type"]}" x:Key="{p["prop"]}">[DirectCast({variable}_converted, {CAST[p["type"]]})]</InArgument>'
        for p in activity['outputs']:
            local = f'CustomActivityStep{n}{p["prop"]}_localParameter'
            self.outputs.append((p['type'], local))
            arguments += f'<OutArgument x:TypeArguments="{p["type"]}" x:Key="{p["prop"]}">[{local}]</OutArgument>'
            if p['type'] == 'mxs:EntityReference':
                entity = (p['entities'] or [OUTPUT_ENTITY[p['prop']]])[0]
                key = created(f'CustomActivityStep{n}{p["prop"]}_entity')
                after += (f'<If Condition="[Microsoft.VisualBasic.IsNothing({local})]"><If.Then><Assign x:TypeArguments="mxs:Entity" To="[{key}]" Value="[New Entity()]" /></If.Then>'
                          f'<If.Else><mxswa:RetrieveEntity Attributes="{{x:Null}}" Entity="[{key}]" EntityId="[DirectCast({local}.Id, System.Guid)]" EntityName="{entity}" ThrowIfNotExists="False" /></If.Else></If>')
        display = f'CustomActivityStep{n}: {label}'
        variables = ''.join(f'<Variable x:TypeArguments="x:Object" Name="{v}" />' for v in self.variables)
        variables = (f'<sco:Collection x:TypeArguments="Variable" x:Key="Variables">{variables}</sco:Collection>' if variables
                     else '<sco:Collection x:TypeArguments="Variable" x:Key="Variables" />')
        return (f'<mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.Composite, {WFA}" DisplayName="{display}">'
                f'<mxswa:ActivityReference.Properties>{variables}<sco:Collection x:TypeArguments="Activity" x:Key="Activities">{body}'
                f'<mxswa:ActivityReference AssemblyQualifiedName="{activity["aqn"]}" DisplayName="{display}">'
                f'<mxswa:ActivityReference.Arguments>{arguments}</mxswa:ActivityReference.Arguments></mxswa:ActivityReference>'
                f'{after}</sco:Collection></mxswa:ActivityReference.Properties></mxswa:ActivityReference>')

    def create(self, entity, label, fields):
        """A Create Record step. fields: [(attribute, type, value)]."""
        n = self._start('CreateStep')
        temp = created(f'CreateStep{n}_localParameter#Temp')
        body = ''
        for attribute, t, value in fields:
            variable = self.temp()
            body += value(self, variable, t)
            body += (f'<mxswa:SetEntityProperty Attribute="{attribute}" Entity="[{temp}]" EntityName="{entity}" Value="[{variable}]">'
                     f'<mxswa:SetEntityProperty.TargetType><InArgument x:TypeArguments="s:Type">{type_literal(t)}</InArgument></mxswa:SetEntityProperty.TargetType></mxswa:SetEntityProperty>')
        variables = ''.join(f'<Variable x:TypeArguments="x:Object" Name="{v}" />' for v in self.variables)
        display = f'CreateStep{n}: {label}'
        return (f'<Sequence DisplayName="{display}"><Sequence.Variables>{variables}</Sequence.Variables>'
                f'<Assign x:TypeArguments="mxs:Entity" To="[{temp}]" Value="[New Entity(&quot;{entity}&quot;)]" />{body}'
                f'<mxswa:CreateEntity EntityId="{{x:Null}}" DisplayName="{display}" Entity="[{temp}]" EntityName="{entity}" />'
                f'<Assign x:TypeArguments="mxs:Entity" To="[{created(f"CreateStep{n}_localParameter")}]" Value="[{temp}]" /><Persist /></Sequence>')

    def email(self, subject, to=None):
        """Create Record: Email, regarding the workflow's record."""
        fields = [('subject', 'x:String', Text(subject))]
        if to:
            def party(b, variable, t):
                who = b.temp()
                return to(b, who, 'mxs:EntityReference') + expression('CreateCrmType', f'Microsoft.Xrm.Sdk.Workflow.WorkflowPropertyType.PartyList, {who}', 'mxs:EntityCollection', variable)
            fields.append(('to', 'mxs:EntityCollection', party))
        fields.append(('regardingobjectid', 'mxs:EntityReference', ThisRecord()))
        return self.create('email', subject, fields)

    def note(self, title, lines):
        """The log note on the workflow's record. lines: (label, value) pairs, one per line."""
        parts = []
        for i, (label, value) in enumerate(lines):
            parts += [('' if i == 0 else '\n') + f'{label}: ', value]
        fields = [('isdocument', 'x:Boolean', No()), ('stepid', 'x:String', Text(f'CreateStep{self.step + 1}')), ('subject', 'x:String', Text(title))]
        if parts:
            fields.append(('notetext', 'x:String', Join(*parts)))
        fields += [('isprivate', 'x:Boolean', No()), ('isautonomouslycreated', 'x:Boolean', No()), ('iscompressed', 'x:Boolean', No()),
                   ('objectid', 'mxs:EntityReference', ThisRecord())]
        return self.create('annotation', 'Log note', fields)


def workflow(id, primary, build):
    """The whole XAML of a workflow. build(builder) returns the steps."""
    builder = Builder(primary)
    steps = ''.join(build(builder))
    variables = ''.join(f'<Variable x:TypeArguments="{t}" Default="{VARIABLE_DEFAULT[t]}" Name="{n}" />' for t, n in builder.outputs)
    head = HEADER.replace('{cls}', 'XrmWorkflow' + id.replace('-', ''))
    xaml = f'{head}<mxswa:Workflow><mxswa:Workflow.Variables>{variables}</mxswa:Workflow.Variables>{steps}</mxswa:Workflow></Activity>'
    xml.etree.ElementTree.fromstring(xaml[xaml.index('?>') + 2:])
    return xaml
