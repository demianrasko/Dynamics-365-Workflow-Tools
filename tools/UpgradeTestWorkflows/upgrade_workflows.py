"""The upgrade test workflows (testing\\Upgrade test workflows.md), 03b to 16 and WFT Test Lead.

Run by tools\\Publish-UpgradeTestWorkflows.ps1: python upgrade_workflows.py <input.json> <output folder>
input.json has the org URL, the installed activities' registrations, the test records and the existing workflows' ids.
It writes one .xaml per workflow and manifest.json.
"""
import json
import os
import sys
import uuid
from wfgen import *

W = 'msdyncrmWorkflowTools.'
C = 'msdyncrmWorkflowTools.Class.'


def definitions(org, r):
    def url(record):
        return record['url']

    team, role = Lookup(r['team']), Lookup(r['role'])
    queue, bpf = Lookup(r['queue']), Lookup(r['bpf'])
    # the application user the script connects as, so no real person is shared with or loses a role
    test_user = Lookup(r['testUser'])
    user = Output(1, 'InitiatingUser')

    def wf03b(b):
        clone_url = Join(r['accountUrlStart'], Output(1, 'ClonedGuid'), '&pagetype=entityrecord')
        relationship = Text('contact_customer_accounts')
        return [
            b.custom(W + 'CloneRecord', 'Clone Record', {'ClonningRecordURL': RecordUrl, 'Prefix': Text('COPY '), 'FieldstoIgnore': Text('accountnumber')}),
            b.custom(W + 'CloneChildren', 'Clone Children', {'SourceRecordUrl': RecordUrl, 'TargetRecordUrl': clone_url, 'RelationshipName': relationship,
                                                             'NewParentFieldNameToUpdate': Text('parentcustomerid')}),
            b.custom(W + 'UpdateChildRecords', 'Update Child Records: description', {'ParentRecordURL': RecordUrl, 'RelationshipName': relationship,
                                                                                   'ValueToSet': Text('wft updated'), 'ChildFieldNameToUpdate': Text('description'),
                                                                                   'UpdateonlyActive': Yes()}),
            b.custom(W + 'UpdateChildRecords', 'Update Child Records: telephone', {'ParentRecordURL': RecordUrl, 'RelationshipName': relationship,
                                                                                 'ParentFieldNameToUpdate': Text('telephone1'), 'ChildFieldNameToUpdate': Text('telephone2'),
                                                                                 'UpdateonlyActive': Yes()}),
            b.custom(W + 'SetLookupFieldFromRecordUrl', 'Set Lookup Field from Record URL', {'RecordUrl': Text(url(r['contact1'])), 'LookupFieldName': Text('primarycontactid')}),
            b.custom(C + 'DeleteRecordAuditHistory', 'Delete Record Audit History', {'RecordURL': clone_url}),
            b.custom(C + 'DeleteRecord', 'Delete Record', {'DeleteUsingRecordURL': No(), 'EntityTypeName': Text('account'), 'EntityGuid': Output(1, 'ClonedGuid')}),
            b.note('WFT 03b Clone and child records', [('Cloned Guid', Output(1, 'ClonedGuid'))]),
        ]

    def wf04(b):
        return [b.custom(W + 'SetState', 'Set State', {'State': Number(1), 'Status': Number(2)}),
                b.note('WFT 04 Status', [])]

    def wf05(b):
        choice = {'GlobalOptionSet': No(), 'AttributeName': Text('new_wfttestchoice'), 'EntityName': Text('account')}
        return [
            b.custom(W + 'GetOptionSetValue', 'Get Option Set Value', {'SourceRecordUrl': RecordUrl, 'AttributeName': Text('new_wfttestchoice')}),
            b.custom(W + 'GetMultiSelectOptionSet', 'Get Multi Select OptionSet', {'SourceRecordUrl': RecordUrl, 'AttributeName': Text('new_wfttestchoices'),
                                                                                   'RetrieveOptionsNames': Yes()}),
            b.custom(W + 'SetMultiSelectOptionSet', 'Set Multi Select OptionSet', {'TargetRecordUrl': RecordUrl, 'AttributeName': Text('new_wfttestchoices'),
                                                                                   'AttributeValues': Text(str(r['choiceTwo'])), 'KeepExistingValues': Yes()}),
            b.custom(W + 'MapMultiSelectOptionSet', 'Map Multi Select OptionSet', {'SourceRecordUrl': RecordUrl, 'SourceAttributes': Text('new_wfttestchoices'),
                                                                                   'TargetRecordUrl': Text(url(r['scratch'])), 'TargetAttributes': Text('new_wfttestchoices')}),
            b.custom(W + 'InsertOptionValue', 'Insert Option Value', dict(choice, OptionText=Text('WFT Extra'), OptionValue=Number(100000900), LanguageCode=Number(1033))),
            b.custom(W + 'DeleteOptionValue', 'Delete Option Value', dict(choice, OptionValue=Number(100000900))),
            b.note('WFT 05 Option sets', [('Value', Output(1, 'SelectedValue')), ('Selected Values', Output(2, 'SelectedValues')),
                                          ('Selected Names', Output(2, 'SelectedNames'))]),
        ]

    def wf06(b):
        lead, relationship = Text(url(r['lead'])), Text('accountleads_association')
        # Check Associate Entity's "Relationship Name" is the intersect table
        intersect = Text('accountleads')
        return [
            b.custom(W + 'AssociateEntity', 'Associate Entity', {'RecordURL': lead, 'RelationshipName': relationship, 'RelationshipEntityName': Text('accountleads')}),
            b.custom(W + 'CheckAssociateEntity', 'Check Associate Entity', {'RecordURL': lead, 'RelationshipName': intersect}),
            b.custom(W + 'DisassociateEntity', 'Disassociate Entity', {'RecordURL': lead, 'RelationshipName': relationship}),
            b.custom(W + 'CheckAssociateEntity', 'Check Associate Entity again', {'RecordURL': lead, 'RelationshipName': intersect}),
            b.note('WFT 06 Relationships', [('Associated', Output(2, 'Result')), ('After Disassociate', Output(4, 'Result'))]),
        ]

    def wf07(b):
        return [
            b.custom(C + 'GetInitiatingUser', 'Get Initiating User'),
            b.custom(W + 'RetrieveUserBUDefaultTeam', 'Retrieve User BU Default Team', {'User': user}),
            b.custom(W + 'CheckUserInTeam', 'Check User In Team', {'Team': team, 'User': user}),
            b.custom(C + 'IsMemberOfTeam', 'Is Member Of Team', {'Team': team, 'User': user}),
            b.custom(W + 'CreateTeam', 'Create Team', {'TeamName': Text('WFT Created Team'), 'TeamType': Number(0), 'Administrator': user,
                                                       'BusinessUnit': Lookup(r['rootBusinessUnit'])}),
            b.custom(W + 'AddUserToTeam', 'Add User To Team', {'Team': Output(5, 'createdTeam'), 'User': user}),
            b.custom(W + 'RemoveUserFromTeam', 'Remove User From Team', {'Team': Output(5, 'createdTeam'), 'User': user}),
            b.custom(W + 'AddRoleToTeam', 'Add Role To Team', {'Role': role, 'Team': team}),
            b.custom(W + 'RemoveRoleFromTeam', 'Remove Role From Team', {'Role': role, 'Team': team}),
            b.custom(W + 'CheckUserInRole', 'Check User In Role', {'Role': role, 'User': user}),
            b.custom(W + 'AddRoleToUser', 'Add Role To User', {'Role': Lookup(r['basicUser']), 'User': user}),
            b.custom(W + 'RemoveRoleFromUser', 'Remove Role From User', {'Role': role, 'User': test_user}),
            b.custom(W + 'SetUserSettings', 'Set User Settings', {'User': user, 'PagingLimit': Number(50)}),
            b.note('WFT 07 Users, teams and roles', [
                ('Initiating User', OutputColumn(1, 'InitiatingUser', 'systemuser', 'fullname')),
                ('Default Team', OutputColumn(2, 'DefaultTeam', 'team', 'name')),
                ('isUserInTeam', Output(3, 'isUserInTeam')),
                ('Is Member Of Team', Output(4, 'Result')),
                ('Created Team', OutputColumn(5, 'createdTeam', 'team', 'name')),
                ('isUserInRole', Output(10, 'isUserInRole'))]),
        ]

    def wf08(b):
        return [
            b.custom(W + 'ShareRecordWithTeam', 'Share Record With Team', {'SharingRecordURL': RecordUrl, 'Team': team, 'ShareRead': Yes(), 'ShareWrite': Yes()}),
            b.custom(W + 'ShareRecordWithUser', 'Share Record With User', {'SharingRecordURL': RecordUrl, 'User': test_user, 'ShareRead': Yes()}),
            b.custom(W + 'ShareSecuredField', 'Share Secured Field', {'RecordURL': RecordUrl, 'AttributeName': Text('new_wfttestsecret'), 'TeamToShare': team,
                                                                      'AllowRead': Yes(), 'AllowUpdate': No()}),
            b.custom(W + 'UnshareRecordWithTeam', 'Unshare Record With Team', {'SharingRecordURL': RecordUrl, 'Team': team}),
            b.custom(W + 'UnshareRecordWithUser', 'Unshare Record With User', {'SharingRecordURL': RecordUrl, 'User': test_user}),
            b.note('WFT 08 Sharing', []),
        ]

    def wf09(b):
        first, second = CreatedRecord(1, 'email'), CreatedRecord(6, 'email')
        return [
            b.email('WFT email test', Lookup(r['contact1'])),
            b.custom(C + 'EmailToTeam', 'Email To Team', {'Email': first, 'Team': team}),
            b.custom(C + 'EntityAttachmentToEmail', 'Entity Attachment To Email', {'MainRecordURL': RecordUrl, 'FileName': Text('*.txt'), 'Email': first,
                                                                                   'RetrieveActivityMimeAttachment': No(), 'MostRecent': No()}),
            b.custom(C + 'SalesLiteratureToEmail', 'Sales Literature To Email', {'Email': first, 'SalesLiterature': Lookup(r['literature']), 'FileName': Text('*')}),
            b.custom(C + 'SendEmail', 'Send Email', {'SourceEmail': first}),
            b.email('WFT role email'),
            b.custom(C + 'SendEmailToUsersInRole', 'Send Email To Users In Role', {'Email': second, 'SecurityRoleLookup': role}),
            b.custom(C + 'SendEmailFromTemplateToUsersInRole', 'Send Email From Template To Users In Role', {'EmailTemplateLookup': Lookup(r['template']),
                                                                                                           'SecurityRoleLookup': role}),
            b.note('WFT 09 Email', [('Email Subject', Output(5, 'Subject'))]),
        ]

    def wf10(b):
        return [
            b.custom(C + 'SetProcess', 'Set Process', {'ClonningRecordURL': RecordUrl, 'Process': bpf}),
            b.custom(C + 'SetProcessStage', 'Set Process Stage', {'ClonningRecordURL': RecordUrl, 'Process': bpf, 'ProcessStage': Text(r['processStage'])}),
            b.custom(C + 'ExecuteWorkflowByID', 'Execute Workflow By ID', {'RecordID': Text(r['account']['id']), 'Process': Lookup(r['wftTest'])}),
            b.custom(C + 'QueueItemCount', 'Queue Item Count', {'SourceQueue': queue, 'CountOnlyUnassigned': Yes()}),
            b.custom(C + 'PickFromQueue', 'Pick From Queue', {'SourceQueue': queue, 'Quantity': Number(1), 'RemoveItems': No()}),
            b.custom(C + 'QueueItemCount', 'Queue Item Count again', {'SourceQueue': queue, 'CountOnlyUnassigned': Yes()}),
            b.note('WFT 10 Processes and queues', [('Items before', Output(4, 'ItemsCount')), ('Items after', Output(6, 'ItemsCount'))]),
        ]

    def wf11a(b):
        this, first, second = ThisRecord(), Lookup(r['list']), Lookup(r['list2'])
        return [
            b.custom(C + 'AddToMarketingList', 'Add To Marketing List', {'MarketingList': first, 'account': this}),
            b.custom(C + 'IsMemberOfMarketingList', 'Is Member Of Marketing List', {'MarketingList': first}),
            b.custom(C + 'CopyMarketingListMembers', 'Copy Marketing List Members', {'SourceList': first, 'TargetList': second}),
            b.custom(C + 'RemoveFromMarketingList', 'Remove From Marketing List', {'MarketingList': first, 'account': this}),
            b.custom(C + 'AddToMarketingList', 'Add To Marketing List again', {'MarketingList': first, 'account': this}),
            b.custom(C + 'RemoveFromAllMarketingLists', 'Remove From All Marketing Lists'),
            b.custom(C + 'IsMemberOfMarketingList', 'Is Member Of Marketing List 2', {'MarketingList': second}),
            b.custom(C + 'AddMarketingListToCampaign', 'Add Marketing List To Campaign', {'MarketingList': first, 'Campaign': Lookup(r['campaign'])}),
            b.custom(C + 'CopyToStaticList', 'Copy To Static List', {'MarketingList': Lookup(r['dynamicList'])}),
            b.custom(W + 'CalculatePrice', 'Calculate Price', {'TargetRecordURL': Text(url(r['opportunity']))}),
            b.custom(W + 'GoalRecalculate', 'Goal Recalculate', {'Goal': Lookup(r['goal'])}),
            b.note('WFT 11a Sales and marketing', [('Member of WFT List', Output(2, 'MemberOfMarketingList')),
                                                   ('Member of WFT List 2', Output(7, 'MemberOfMarketingList'))]),
        ]

    def wf11b(b):
        return [b.custom(W + 'QualifyLead', 'Qualify Lead', {'Lead': ThisRecord(), 'CreateAccount': Yes(), 'CreateContact': Yes(), 'CreateOpportunity': Yes(),
                                                             'LeadStatus': Number(3)}),
                b.note('WFT 11b Qualify lead', [])]

    def wf11c(b):
        return [b.custom(W + 'ApplyRoutingRule', 'Apply Routing Rule', {'IncidentRecordURL': RecordUrl}),
                b.custom(C + 'ResolveCase', 'Resolve Case', {'Incident': ThisRecord(), 'IncidentResolution': Text('WFT resolved'), 'ResolutionDescription': Text('wft')}),
                b.note('WFT 11c Case', [])]

    def wf12(b):
        return [
            b.custom(W + 'OrgDBSettingsRetrieve', 'OrgDB Settings Retrieve', {'orgDBSetting': Text('trackingprefix')}),
            b.custom(W + 'OrgDBSettingsUpdate', 'OrgDB Settings Update', {'orgDBSetting': Text('trackingprefix'), 'Value': Output(1, 'StringValue')}),
            b.custom(W + 'GetAppModuleID', 'Get App Module ID', {'AppModuleUniqueName': Text('msdynce_saleshub')}),
            b.custom(W + 'GetAppRecordUrl', 'Get App Record Url', {'RecordURL': RecordUrl, 'AppModuleUniqueName': Text('msdynce_saleshub')}),
            b.custom(W + 'GetSharepointLocationURL', 'Get Sharepoint Location URL', {'RecordURL': RecordUrl}),
            b.custom(W + 'EntityJsonSerializer', 'Entity Json Serializer', {'SerializingRecordURL': RecordUrl}),
            b.custom(W + 'CalculateRollupField', 'Calculate Rollup Field', {'ParentRecordURL': RecordUrl, 'FieldName': Text('opendeals')}),
            b.note('WFT 12 Settings, apps and SharePoint', [('String Value', Output(1, 'StringValue')), ('App Module ID', Output(3, 'AppModuleId')),
                                                            ('App Record Url', Output(4, 'AppRecordUrl')), ('SharePoint Location URL', Output(5, 'SharepointLocationURL')),
                                                            ('Json', Output(6, 'OutputJson'))]),
        ]

    def wf13(b):
        return [
            b.custom(W + 'CurrencyConvert', 'Currency Convert', {'Amount': Number(100), 'FromCurrency': Text('EUR'), 'ToCurrency': Text('USD')}),
            b.custom(W + 'TranslateText', 'Translate Text', {'TextToTranslate': Text('Hola'), 'Language': Text('pt')}),
            b.custom(W + 'GeoCodeAddress', 'Geocode Address', {'Address': Column('address1_composite', 'x:String')}),
            b.note('WFT 13 External services', [('Currency', Output(1, 'Result')), ('Translated', Output(2, 'TranslatedText')),
                                                ('Latitude', Output(3, 'Latitude')), ('Longitude', Output(3, 'Longitude'))]),
        ]

    # ---- the activities added since 1.0.61.1 --------------------------------------------------------------------

    def wf14(b):
        return [
            b.custom(W + 'AISummarizeRecord', 'AI Summarize Record', {'RecordUrl': RecordUrl}),
            b.custom(W + 'AISummarizeText', 'AI Summarize Text', {'TextToSummarize': Text(
                'The customer called about a late delivery. The order left the warehouse on Monday but the carrier lost the '
                'tracking. We sent a replacement by express and offered a 10% discount on the next order.')}),
            b.custom(W + 'AIClassifyText', 'AI Classify Text', {'TextToClassify': Text('My invoice shows the wrong amount.'),
                                                                'CategoriesCsv': Text('Billing,Support,Sales')}),
            b.custom(W + 'AISentimentDetect', 'AI Sentiment Detect', {'TextToAnalyzeSentiment': Text('I love the new product, thank you!')}),
            b.custom(W + 'AIDraftReply', 'AI Draft Reply', {'TextToReplyTo': Text('Can you send me the price list for next year?')}),
            b.custom(W + 'AITranslateText', 'AI Translate Text', {'TextToTranslate': Text('Hello, how are you?'), 'TargetLanguage': Text('fr')}),
            b.note('WFT 14 AI', [line for step, label, output in [
                (1, 'Summarize Record', 'SummaryText'), (2, 'Summarize Text', 'SummaryText'), (3, 'Classify', 'TopCategory'),
                (4, 'Sentiment', 'Sentiment'), (5, 'Draft Reply', 'ReplyText'), (6, 'Translate', 'TranslatedText')]
                for line in [(label, Output(step, output)), (f'{label} failed', Output(step, 'Failed')), (f'{label} error', Output(step, 'FailureMessage'))]]),
        ]

    def wf15a(b):
        return [
            b.custom(W + 'CreateOpportunityProduct', 'Create Opportunity Product', {'Opportunity': ThisRecord(), 'ExistingProduct': Lookup(r['product']),
                                                                                   'UoM': Lookup(r['uom']), 'Quantity': Number(3)}),
            b.custom(W + 'CreateQuoteFromOpportunity', 'Create Quote From Opportunity', {'Opportunity': ThisRecord()}),
            b.custom(W + 'UpdateQuoteValue', 'Update Quote Value', {'Quote': Output(2, 'Quote'), 'Discountamount': Number(5), 'Fieldname': Text('discountamount')}),
            b.note('WFT 15a Quotes', [('Quote', OutputColumn(2, 'Quote', 'quote', 'name'))]),
        ]

    def wf15b(b):
        return [b.custom(W + 'WinQuote', 'Win Quote', {'Quote': ThisRecord(), 'Message': Text('WFT won')}),
                b.note('WFT 15b Win quote', [])]

    # quote products have no notes, so 15c has no log note
    def wf15c(b):
        return [b.custom(W + 'UpdateProductQuoteValue', 'Update Product Quote Value', {'Quote': ThisRecord(), 'Discountamount': Number(1),
                                                                                      'Fieldname': Text('manualdiscountamount')})]

    # run by 16 on WFT Lead
    def wf_test_lead(b):
        return [b.note('WFT Test Lead ran', [])]

    def wf16(b):
        lead = Text(url(r['lead']))
        test_lead = Lookup({'entity': 'workflow', 'id': r['workflowIds']['WFT Test Lead'], 'name': 'WFT Test Lead'})
        leads_query = "<fetch><entity name='lead'><attribute name='leadid' /><filter><condition attribute='emailaddress1' operator='eq' value='wft-lead@example.com' /></filter></entity></fetch>"
        return [
            b.custom(W + 'GetEnvironmentVariable', 'Get Environment Variable', {'SchemaName': Text('new_wfttestvariable')}),
            b.custom(W + 'GetRecordUrl', 'Get Record URL', {'ReferenceRecordUrl': RecordUrl, 'RecordId': Text(r['contact1']['id']), 'EntityName': Text('contact')}),
            b.custom(W + 'ExecuteWorkflowForRecordsinQuery', 'Execute Workflow For Records in Query', {'Process': test_lead, 'Query': Text(leads_query)}),
            b.custom(W + 'DistributeWFActivityOneToMany', 'Distribute Workflow 1:N', {'RelationshipName': Text('account_parent_account'), 'Workflow': Lookup(r['wftTest'])}),
            b.custom(W + 'AssociateEntity', 'Associate WFT Lead', {'RecordURL': lead, 'RelationshipName': Text('accountleads_association'),
                                                                  'RelationshipEntityName': Text('accountleads')}),
            b.custom(W + 'DistributeWFActivityManyToMany', 'Distribute Workflow N:N', {'RelationshipName': Text('accountleads_association'), 'Workflow': test_lead}),
            b.custom(W + 'DisassociateEntity', 'Disassociate WFT Lead', {'RecordURL': lead, 'RelationshipName': Text('accountleads_association')}),
            b.note('WFT 16 Utilities', [('Variable', Output(1, 'Value')), ('Found', Output(1, 'Found')), ('Record URL', Output(2, 'RecordUrl'))]),
        ]

    return [
        ('WFT 03b Clone and child records', 'account', wf03b), ('WFT 04 Status', 'account', wf04), ('WFT 05 Option sets', 'account', wf05),
        ('WFT 06 Relationships', 'account', wf06), ('WFT 07 Users, teams and roles', 'account', wf07), ('WFT 08 Sharing', 'account', wf08),
        ('WFT 09 Email', 'account', wf09), ('WFT 10 Processes and queues', 'contact', wf10), ('WFT 11a Sales and marketing', 'account', wf11a),
        ('WFT 11b Qualify lead', 'lead', wf11b), ('WFT 11c Case', 'incident', wf11c), ('WFT 12 Settings, apps and SharePoint', 'account', wf12),
        ('WFT 13 External services', 'account', wf13),
        ('WFT 14 AI', 'account', wf14), ('WFT 15a Quotes', 'opportunity', wf15a), ('WFT 15b Win quote', 'quote', wf15b),
        ('WFT 15c Quote product discount', 'quotedetail', wf15c), ('WFT Test Lead', 'lead', wf_test_lead), ('WFT 16 Utilities', 'account', wf16),
    ]


def main(input_file, output_folder):
    data = json.load(open(input_file, encoding='utf-8-sig'))
    load_activities(data['activities'])
    os.makedirs(output_folder, exist_ok=True)
    manifest = []
    # every workflow's id, new ones too, before any is built: a step can run another of them (16 runs WFT Test Lead)
    ids = dict(data['workflows'])
    data['records']['workflowIds'] = ids
    workflows = definitions(data['org'], data['records'])
    for name, primary, build in workflows:
        ids.setdefault(name, str(uuid.uuid4()))
    for name, primary, build in workflows:
        id = ids[name]
        file = os.path.join(output_folder, name + '.xaml')
        with open(file, 'w', encoding='utf-8', newline='') as f:
            f.write(workflow(id, primary, build))
        manifest.append(dict(name=name, id=id, primary=primary, file=file, new=name not in data['workflows']))
    with open(os.path.join(output_folder, 'manifest.json'), 'w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=1)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
