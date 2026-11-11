using UnityEngine;

public enum PortObstacleKind { Structure,Worker,Vehicle,Cargo,Deck }
public class PortObstacle : MonoBehaviour
{
    public string actorId;
    public PortObstacleKind kind;
    public Collider shape;
    public string Id=>string.IsNullOrEmpty(actorId)?name:actorId;
}
