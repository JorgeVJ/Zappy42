using Godot;

/// <summary>
/// Base de los animales cuyo modelo trae un AnimationPlayer con clips (aves, zorros…).
/// Reúne la carga del modelo, la resolución de su AnimationPlayer y la reproducción de
/// clips (en bucle o one-shot, con fundido opcional), que compartían Bird y Fox.
/// </summary>
/// <remarks>
/// Autocontenido: no referencia tipos del proyecto. Los animales animados por huesos
/// (p. ej. Fish) no la usan; derivan directamente de <see cref="Animal"/>.
/// </remarks>
public partial class ClipAnimal : Animal
{
	/// <summary>Nodo raíz del modelo instanciado; disponible para las subclases (p. ej. para el banking).</summary>
	protected Node3D Model;

	private AnimationPlayer _player;
	private string _currentClip;

	/// <summary>AnimationPlayer del modelo (o null si el .glb no trae uno).</summary>
	protected AnimationPlayer Player => _player;

	/// <summary>Instancia el modelo desde <see cref="Animal.ModelPath"/> y resuelve su AnimationPlayer.</summary>
	protected void LoadModelAndPlayer()
	{
		Model = LoadModel();
		if (Model != null)
			_player = FindInDescendants<AnimationPlayer>(Model);
	}

	/// <summary>
	/// Reproduce un clip si no era ya el activo: fija su modo de bucle y lo lanza con el
	/// fundido indicado. No hace nada si no hay AnimationPlayer o el clip no existe.
	/// </summary>
	protected void PlayClip(string clip, bool loop = true, float blend = 0f)
	{
		if (_player == null || string.IsNullOrEmpty(clip) || clip == _currentClip)
			return;
		if (!_player.HasAnimation(clip))
			return;

		Animation animation = _player.GetAnimation(clip);
		if (animation != null)
			animation.LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;

		_player.Play(clip, blend);
		_currentClip = clip;
	}
}
