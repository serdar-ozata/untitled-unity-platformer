public class PlayerConditions {
    public bool IsJumping;
    public bool IsSwinging;
    public bool OnGround;
    public bool IsFacingRight;
    public bool IsClingingWall;
    public bool IsDashed;
    public bool IsDashing;
    public bool IsClimbing;
    public bool IsSliding;
    public bool IsExposedToFan;
    public bool IsThereInteractable;
    public bool OnStuckSword;
    public bool OnInteraction;
    public byte ChargeAmount; 
    public void Reset() {
        OnInteraction = false;
        IsJumping = false;
        IsSwinging = false;
        OnGround = false;
        IsFacingRight = true;
        IsClingingWall = false;
        IsDashed = false;
        IsDashing = false;
        IsClimbing = false;
        IsSliding = false;
        IsExposedToFan = false;
        IsThereInteractable = false;
        OnStuckSword = false;
        ChargeAmount = 0;
    }
}