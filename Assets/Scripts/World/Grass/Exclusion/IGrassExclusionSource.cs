namespace AdanBye.Grass
{
    /// <summary>
    /// Çimin çıkmaması gereken bir alan türü (ağaç, göl...). Neden interface: baker kaynakları
    /// sırayla canvas'a yazar, yeni tür eklemek baker'ı değiştirmez (OCP).
    /// </summary>
    public interface IGrassExclusionSource
    {
        void Rasterize(ExclusionMaskCanvas canvas);
    }
}
