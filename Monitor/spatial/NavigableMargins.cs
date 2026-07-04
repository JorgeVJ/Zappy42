/// <summary>
/// Márgenes que un dominio navegable deja respecto al fondo y a la superficie
/// del volumen en el que se puede mover un animal.
/// </summary>
public readonly struct NavigableMargins
{
	public readonly float Floor;
	public readonly float Surface;

	public NavigableMargins(float floor, float surface)
	{
		Floor = floor;
		Surface = surface;
	}
}
