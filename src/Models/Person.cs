namespace EmotionUpdaterWS.src.Models;

public class Person
{
    public int Id { get; set; }
    public Dictionary<string, int>? Emotions { get; set; }
}