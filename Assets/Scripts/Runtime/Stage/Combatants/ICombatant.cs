using UnityEngine;

public interface ICombatant
{
    ITeamMember TeamMember { get; }

    Transform TargetTransform { get; }

    bool IsTargetable { get; }
}
