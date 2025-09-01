using UnityEngine;

public interface IPlayerPositioner
{
    bool PositionPlayer(Vector3 targetPosition, Vector3 spawnOffset);
}