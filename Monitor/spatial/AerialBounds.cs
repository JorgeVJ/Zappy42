/// <summary>
/// Límites verticales del volumen aéreo de <see cref="AerialDomain"/>: margen de orilla
/// para distinguir tierra de agua, altura mínima de vuelo sobre el suelo y techo absoluto.
/// </summary>
public readonly struct AerialBounds
{
	public readonly float ShoreMargin;
	public readonly float MinFlyAltitude;
	public readonly float Ceiling;

	public AerialBounds(float shoreMargin, float minFlyAltitude, float ceiling)
	{
		ShoreMargin = shoreMargin;
		MinFlyAltitude = minFlyAltitude;
		Ceiling = ceiling;
	}
}
