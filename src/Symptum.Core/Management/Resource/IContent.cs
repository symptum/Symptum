namespace Symptum.Core.Management.Resource
{
    public interface IContent
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Authors { get; set; }

        public ContentFileType ContentFileType { get; set; }

        public DateOnly DateModified { get; set; }

        public IList<string> References { get; set; }

        public IList<string> Tags { get; set; }

        public IList<string> SeeAlso { get; set; }

        public string Dependencies { get; set; }
    }
}