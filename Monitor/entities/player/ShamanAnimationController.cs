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

    // Duraciones reales de los clips (en segundos, sin escalar por SpeedScale),
    // tomadas del propio AnimationPlayer. Las usan los handlers de red (pic/pgt/pdr)
    // para saber cuánto esperar antes de volver a Idle (PlayOneShot) o, en el caso
    // de pic, antes de recibir pie con el resultado de la incantación.
    public float SpellDuration   => GetClipLength(Clip.SpellCast);
    public float CollectDuration => GetClipLength(Clip.CollectObj);
    public float PickUpDuration  => GetClipLength(Clip.PickUpPocket);

    // Para que PlayOneShot (Player.cs) no fuerce Idle si, mientras esperaba el
    // timer, ya se inició otra animación distinta (p.ej. una incantación llegó
    // mientras terminaba el gesto de recoger/dejar recurso).
    public bool IsPlayingCollect => _anim?.CurrentAnimation == Clip.CollectObj;
    public bool IsPlayingPickUp  => _anim?.CurrentAnimation == Clip.PickUpPocket;

    // Escala la velocidad de reproducción de TODAS las animaciones (acelera/ralentiza
    // en función del time unit del servidor).
    public void SetSpeedScale(float scale)
    {
        if (_anim != null)
            _anim.SpeedScale = Mathf.Max(0.01f, scale);
    }

    private void TryPlay(string name)
    {
        if (_anim == null || !_anim.HasAnimation(name))
            return;
        if (_anim.CurrentAnimation == name)
            return; // ya se está reproduciendo; evita reiniciarla cada frame
        _anim.Play(name);
    }

    // Duración del clip en segundos (0 si no existe), independiente del SpeedScale
    // actual; usada por Player.cs para temporizar el regreso a Idle tras un one-shot.
    private float GetClipLength(string name)
    {
        if (_anim == null || !_anim.HasAnimation(name))
            return 0f;
        var anim = _anim.GetAnimation(name);
        return anim != null ? (float)anim.Length : 0f;
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
