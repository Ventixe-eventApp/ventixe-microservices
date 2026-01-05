namespace Ventixe.AI.Service.Models;

public class EventModel
{
    public string Id { get; set; } = null!;
    public string EventName { get; set; } = null!;
    public string ArtistName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Location { get; set; } = null!;
    public DateTime StartDate { get; set; }

    public string ToEmbeddingText()
        => $"Event: {EventName}. Artist: {ArtistName}. Location: {Location}. Date: {StartDate:yyyy-MM-dd}. Description: {Description}";

}
