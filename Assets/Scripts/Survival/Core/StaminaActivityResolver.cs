namespace AdanBye.Survival
{
    // Motorun hız/koşu bilgisinden stamina aktivitesini türetir; saf, Unity'siz.
    public static class StaminaActivityResolver
    {
        // Bu eşiğin altındaki yatay hız "duruyor" sayılır (SmoothDamp artık titremesi Walk'a sayılmasın).
        public const float MovingSpeedThreshold = 0.1f;

        public static StaminaActivity Resolve(float horizontalSpeed, bool isRunning)
        {
            if (!(horizontalSpeed > MovingSpeedThreshold)) return StaminaActivity.Idle;
            return isRunning ? StaminaActivity.Run : StaminaActivity.Walk;
        }
    }
}
