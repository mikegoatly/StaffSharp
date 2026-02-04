using StaffSharp.Core.Notation;
using StaffSharp.Performance;

namespace StaffSharp.TestHelpers.Builders;

public static class PerformanceEventExtensions
{
    public static IList<VoiceAssignment> AssignToVoice(this IEnumerable<IPerformanceEvent> events, int voiceNumber = 1)
    {
        return [.. events.Select(e => new VoiceAssignment(e, voiceNumber))];
    }

    public static Dictionary<int, List<VoiceAssignment>> ToVoiceDictionary(this IEnumerable<VoiceAssignment> assignments, params IEnumerable<VoiceAssignment>[] otherAssignments)
    {
        var dict = new Dictionary<int, List<VoiceAssignment>>
        {
            [assignments.First().VoiceNumber] = [.. assignments]
        };

        foreach (var voiceAssignment in otherAssignments)
        {
            var voiceNumber = voiceAssignment.First().VoiceNumber;
            dict[voiceNumber] = [.. voiceAssignment];
        }

        return dict;
    }
}
