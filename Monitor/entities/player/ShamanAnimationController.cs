using Godot;

public sealed class ShamanAnimationController
{
    private static class Clip
    {
        public const string Idle         = "idle";
        public const string Walking      = "walking";
        public const string Running      = "running";
        public const string SpellCast    = "spell_cast";
        public const string CollectObj   = "collect_object";
        public const string PickUpPocket = "pick_up_pocket";
    }

    private readonly AnimationPlayer _anim;

    public ShamanAnimationController(AnimationPlayer anim)
    {
        _anim = anim;
        EnableLoopOnAll();
        PlayIdle();
    }

    public void PlayIdle()    => TryPlay(Clip.Idle);
    public void PlayWalk()    => TryPlay(Clip.Walking);
    public void PlayRun()     => TryPlay(Clip.Running);
    public void PlaySpell()   => TryPlay(Clip.SpellCast);
    public void PlayCollect() => TryPlay(Clip.CollectObj);
    public void PlayPickUp()  => TryPlay(Clip.PickUpPocket);

    // Escala la velocidad de reproducción de TODAS las animaciones (acelera/ralentiza
    // en función del time unit del servidor).
    public void SetSpeedScale(float scale)
    {
        if (_anim != null)
            _anim.SpeedScale = Mathf.Max(0.01f, scale);
    }

    private void TryPlay(string name)
    {
        if (_anim != null && _anim.HasAnimation(name))
            _anim.Play(name);
    }

    private void EnableLoopOnAll()
    {
        foreach (StringName name in _anim.GetAnimationList())
        {
            var anim = _anim.GetAnimation(name);
            if (anim != null)
                anim.LoopMode = Animation.LoopModeEnum.Linear;
        }
    }
}
