// Per target contact, independent of feedback visibility and the final trigger time.
public sealed class Study3PendingStateTimer
{
    private int contacts;
    private double contactTime;
    public double? ElapsedSeconds { get; private set; }

    public void Contact(int finger, bool inside, double now)
    {
        if (inside)
        {
            if (contacts == 0)
            {
                contactTime = now;
                ElapsedSeconds = null;
            }
            contacts |= finger;
        }
        else contacts &= ~finger;
        // Retain the result on exit: exit-to-confirm can finish in the same callback.
    }

    public void EnterPending(double now)
    {
        if (contacts == 0 || ElapsedSeconds.HasValue || double.IsNaN(now) || double.IsInfinity(now)) return;
        if (!double.IsNaN(contactTime) && !double.IsInfinity(contactTime) && now >= contactTime)
            ElapsedSeconds = now - contactTime;
    }

    public void Reset()
    {
        contacts = 0;
        contactTime = 0;
        ElapsedSeconds = null;
    }
}
