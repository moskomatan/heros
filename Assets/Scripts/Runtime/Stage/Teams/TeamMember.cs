using System;
using UnityEngine;

[Serializable]
public sealed class TeamMember : ITeamMember
{
    [SerializeField] private TeamId _team;

    public TeamId Team => _team;

    public void SetTeam(TeamId team)
    {
        _team = team;
    }
}
