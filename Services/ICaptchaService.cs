namespace Services
{
    public interface ICaptchaService
    {
        Task<bool> VerifyCaptchaAsync(string captchaToken);
    }
}
