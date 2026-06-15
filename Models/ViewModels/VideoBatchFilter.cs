namespace GurudevDefenceAcademy.Models.ViewModels;

// One chip on the student "Video Lectures" filter bar: a batch + how many videos it has.
public record VideoBatchFilter(int BatchId, string Name, int Count);
