namespace VMLab.Model
{
    public interface IPowershellCommandResult
    {
        object Results { get; set; }
        object Errors { get; set; }
    }

    public class PowershellCommandResult : IPowershellCommandResult
    {
        public object Results { get; set; }
        public object Errors { get; set; }
    }
}
