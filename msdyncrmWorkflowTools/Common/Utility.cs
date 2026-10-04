using Microsoft.Crm.Sdk.Messages;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// General-purpose helpers shared by the workflow activities and Common. Keep methods here static and free of workflow state.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Builds the access mask for a GrantAccessRequest from the individual share flags.
        /// </summary>
        /// <param name="read">Grant read access.</param>
        /// <param name="write">Grant write access.</param>
        /// <param name="append">Grant append access.</param>
        /// <param name="appendTo">Grant append-to access.</param>
        /// <param name="delete">Grant delete access.</param>
        /// <param name="share">Grant share access.</param>
        /// <param name="assign">Grant assign access.</param>
        /// <returns>The combined <see cref="AccessRights"/>, or <see cref="AccessRights.None"/> if no flag is set.</returns>
        public static AccessRights GetMask(bool read, bool write, bool append, bool appendTo, bool delete, bool share, bool assign)
        {
            var mask = AccessRights.None;

            if (read) mask |= AccessRights.ReadAccess;
            if (write) mask |= AccessRights.WriteAccess;
            if (append) mask |= AccessRights.AppendAccess;
            if (appendTo) mask |= AccessRights.AppendToAccess;
            if (delete) mask |= AccessRights.DeleteAccess;
            if (share) mask |= AccessRights.ShareAccess;
            if (assign) mask |= AccessRights.AssignAccess;

            return mask;
        }
    }
}
