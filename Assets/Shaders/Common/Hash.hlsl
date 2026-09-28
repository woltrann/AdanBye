#ifndef ADANBYE_HASH_INCLUDED
#define ADANBYE_HASH_INCLUDED

// Deterministik, durumsuz hash yardımcıları (PCG, M. E. O'Neill; pcg-random.org, kamu malı/MIT tarzı algoritma).
// Neden PCG: sin() tabanlı "fract(sin(x)*43758)" hash'leri GPU'ya/derleyiciye göre farklı sonuç verir;
// PCG yalnızca tamsayı çarpma/kaydırma/xor kullanır, her GPU'da ve C# tarafında bit-bit aynıdır.
// Çim yerleşimi bu dosyaya şu sözleşmeyle dayanır: bir hücrenin tüm rastgeleliği YALNIZCA (hücre indeksi, seed)'den
// türer; kamera konumu, chunk seçimi ya da dispatch sırası sonucu etkilemez => kamera kayınca çim titremez.

uint Hash_PCG(uint v)
{
    uint state = v * 747796405u + 2891336233u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (word >> 22u) ^ word;
}

// Dünya hücre indeksi (terrain origin'ine göre tamsayı) + seed -> 32 bit hash.
// İç içe zincir: (x,y) ile (y,x) farklı hash verir (simetri yok).
uint Hash_Cell(int2 cell, uint seed)
{
    return Hash_PCG(asuint(cell.x) ^ Hash_PCG(asuint(cell.y) ^ Hash_PCG(seed)));
}

// Aynı hash'ten bağımsız akışlar: her 'stream' numarası ayrı bir rastgele sayı verir (jitter x, jitter z, rank...).
uint Hash_Stream(uint h, uint stream)
{
    return Hash_PCG(h + stream * 0x9E3779B9u);
}

// [0,1): üst 24 bit float mantisine tam sığar (0..2^24-1) / 2^24; 1.0 asla üretilmez.
float Hash_ToUnit(uint h)
{
    return (h >> 8) * (1.0 / 16777216.0);
}

float Hash_Stream01(uint h, uint stream)
{
    return Hash_ToUnit(Hash_Stream(h, stream));
}

#endif // ADANBYE_HASH_INCLUDED
