namespace Dicom_Viewer.Models
{
    public class DicomStudyModels
    {
        public Guid Id { get; set; }

        public string StudyInstanceUID { get; set; }

        public string PatientName { get; set; }

        public string PatientID { get; set; }

        public string Modalaty { get; set; }

        public string StudyDate { get; set; }

        public string FilePath { get; set; }
    }
}
