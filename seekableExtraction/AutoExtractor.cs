using seekableExtraction.Extractors;

namespace seekableExtraction
{
    public static class AutoExtractor
    {
        /// <summary>
        /// Automatically check compatibility with multiple (currently one, but there will be multiple) extractors;
        /// Return null if there is not a suitable extractor.
        /// Otherwise return a Extractor with option passed.
        /// </summary>
        public static Extractor findExtractor(ExtractorOptions option = null)
        {
            if (Tar.Check_compatibility(option))
                return new Tar(option);
            return null;
        }
    }
}
