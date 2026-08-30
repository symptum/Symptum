namespace Symptum.UI.Controls.Audio;

/// <summary>
/// Decodes supported audio formats into mono float PCM samples (in the -1..1 range)
/// that can be used to build waveform data. Only MP3 is supported because it is the
/// only audio format that the media players on all Uno targets can play.
/// </summary>
internal static class AudioSampleDecoder
{
    /// <summary>
    /// Decodes an audio stream into waveform samples normalized to -1..1.
    /// </summary>
    /// <param name="stream">A seekable audio stream.</param>
    /// <param name="duration">The real media duration in seconds, taken from NLayer's
    /// per-channel sample count so it is independent of the channel layout.</param>
    /// <param name="cancellationToken">A cancellation token for the decoding operation.</param>
    public static List<float> DecodeToSamples(Stream stream, out double duration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var mpeg = new NLayer.MpegFile(stream)
        {
            StereoMode = NLayer.StereoMode.DownmixToMono,
        };

        var sampleRate = mpeg.SampleRate;
        if (sampleRate <= 0)
        {
            duration = 0;
            return [];
        }

        List<float> samples = [with((int)Math.Max(stream.CanSeek ? stream.Length / 2 : 4096, 4096))];
        float[] buffer = new float[8192];
        int read;

        while ((read = mpeg.ReadSamples(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int i = 0; i < read; i++)
            {
                samples.Add(buffer[i]);
            }
        }

        duration = mpeg.Duration.TotalSeconds;
        if (duration <= 0 && sampleRate > 0 && samples.Count > 0)
        {
            var channels = mpeg.Channels > 0 ? mpeg.Channels : 1;
            duration = (double)samples.Count / sampleRate / channels;
        }

        return samples;
    }
}
