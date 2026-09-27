
using Dicom_Viewer.Models;
using Dicom_Viewer.Repositories;
using FellowOakDicom;

namespace Dicom_Viewer.Services
  
{
    public class DicomHapper
    {
        public static string ProcessAndSave(string dicomFilePath , IConfiguration configuration)
        {
            var dicomFile = DicomFile.Open(dicomFilePath);
            var dataSet = dicomFile.Dataset;

            var study = new DicomStudyModels
            {
                Id = Guid.NewGuid(),
                StudyInstanceUID = dataSet.GetSingleValue<string>(DicomTag.StudyInstanceUID),
                PatientName = dataSet.GetSingleValue<string>(DicomTag.PatientName),
                PatientID = dataSet.GetSingleValue<string>(DicomTag.PatientID),
                Modality = dataSet.GetSingleValue<string>(DicomTag.Modality),
                StudyDate = dataSet.GetSingleValue<string>(DicomTag.StudyDate),
                FilePath = dicomFilePath
            };

            var repro = new DicomViewerRepo(configuration);
             
             repro.PostStudy(study).Wait();

            return study.StudyInstanceUID;

        }
    }
}
