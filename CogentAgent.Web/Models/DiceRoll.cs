namespace CogentAgent.Web.Models;

public class DiceRoll
{
    public int DiceCount { get; set; }
    public int Successes { get; set; }
    public List<int> Rolls { get; set; } = new();
}
