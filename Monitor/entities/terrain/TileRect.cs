public partial class DecorationSystem
{
	/// <summary>
	/// Rectángulo de tiles (footprint) ocupado por una decoración: esquina superior-izquierda
	/// (Tx, Ty) y tamaño (W, L) en tiles.
	/// </summary>
	private readonly record struct TileRect(int Tx, int Ty, int W, int L);
}
