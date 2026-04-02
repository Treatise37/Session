using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Sent from server to client to show a closeable job rules window.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ShowJobRulesWindowEvent : EntityEventArgs
    {
        public readonly NetEntity NetEntity;
        public readonly string JobNameLocId;
        public readonly string RulesLocId;

        public ShowJobRulesWindowEvent(NetEntity netEntity, string jobNameLocId, string rulesLocId)
        {
            NetEntity = netEntity;
            JobNameLocId = jobNameLocId;
            RulesLocId = rulesLocId;
        }
    }
}
