namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class CouchDbMidiDevice : MidiDevice
{
    [Newtonsoft.Json.JsonProperty("_id")]
    public string Id { get; set; } = string.Empty;
    [Newtonsoft.Json.JsonProperty("_rev")]
    public string Rev { get; set; } = string.Empty;
}