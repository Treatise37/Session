// stalker-changes
using Robust.Shared.GameStates;

namespace Content.Server.Roles
{
    /// <summary>
    /// Keeps track of the action entity that shows the current job's rules.
    /// </summary>
    [RegisterComponent]
    public sealed partial class JobRulesActionComponent : Component
    {
        /// <summary>
        /// Action entity that is being shown in the UI to allow players to re-open their current job rules.
        /// </summary>
        public EntityUid? ActionEntity;
    }
}
// stalker-changes-end
