namespace DataLibrary.Models;
public enum WasteDisposition { Other,Recycling,Landfill }
public static class WasteCategoryClassifier
{
 public static WasteDisposition Classify(string? category) {
  var text=(category??"").Trim().ToLowerInvariant();
  var recycle=text.Contains("recycl")&&!text.Contains("non")&&!text.Contains("not ")&&!text.Contains("reject");
  var landfill=text.Contains("landfill");
  return recycle==landfill?WasteDisposition.Other:recycle?WasteDisposition.Recycling:WasteDisposition.Landfill;
 }
}
