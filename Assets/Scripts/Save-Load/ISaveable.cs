// Bu deseni implement eden bir sınıf kendi state'ini SaveData'ya yazmayı/okumayı bilir.
// SaveManager artık "MainCharacter'ın hangi alanları var" diye bilmek zorunda değil -
// sadece bu arayüzü implement eden herkese "durumunu yaz" / "durumunu geri yükle" der.
// MainCharacter içindeki bir alan değiştiğinde SaveManager'a HİÇ dokunulmaz.
public interface ISaveable
{
    void CaptureState(SaveData data);
    void RestoreState(SaveData data);
}
