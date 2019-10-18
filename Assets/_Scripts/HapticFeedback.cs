using System.Threading.Tasks;

public static class HapticFeedback
{
    #region Public Static Methods

    public static void HapticPulse(float frequency, float amplitude, OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        Task.Delay(100).ContinueWith(t => CancelHapticPulse(controller));
    }

    public static void CancelHapticPulse(OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(0, 0, controller);
    }

    #endregion
}