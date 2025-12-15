namespace FitnessCenter.Models
{
    // DB'de int olarak saklanır (EF Core enum'u int'e map'ler)
    public enum AppointmentStatus
    {
        Pending = 0,     // Admin onayı bekliyor
        Confirmed = 1,   // Admin onayladı
        Rejected = 2,    // Admin reddetti
        Cancelled = 3,   // Kullanıcı/Admin iptal etti
        Done = 4         // Tamamlandı
    }

    public static class AppointmentStatusText
    {
        public static string ToText(AppointmentStatus status) => status switch
        {
            AppointmentStatus.Pending => "Onay Bekliyor",
            AppointmentStatus.Confirmed => "Onaylandı",
            AppointmentStatus.Rejected => "Reddedildi",
            AppointmentStatus.Cancelled => "İptal Edildi",
            AppointmentStatus.Done => "Tamamlandı",
            _ => "Bilinmiyor"
        };
    }
}
