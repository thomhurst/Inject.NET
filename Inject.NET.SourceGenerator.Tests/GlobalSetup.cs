using DiffEngine;

public class GlobalSetup
{
    [Before(TestSession)]
    public static void SetUp()
    {
        DiffRunner.Disabled = true;
    }
}
