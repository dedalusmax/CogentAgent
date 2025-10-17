namespace CogentAgent.Web.Models;

public class Scene
{
    public string Title { get; set; }
    public string Description { get; set; }
    public List<Character> Participants { get; set; } = new();
    public List<string> Objectives { get; set; } = new();
    public bool IsResolved { get; set; }
}
