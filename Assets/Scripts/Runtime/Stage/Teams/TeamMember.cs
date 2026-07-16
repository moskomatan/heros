using UnityEngine;

public sealed class TeamMember : MonoBehaviour, ITeamMember
{
    [SerializeField] private TeamId _team;

    public TeamId Team => _team;

    public void SetTeam(TeamId team)
    {
        _team = team;
    }
}
