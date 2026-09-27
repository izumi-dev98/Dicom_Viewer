using Dapper;
using Dicom_Viewer.Models;
using System.Collections;
using System.Data.SqlClient;

namespace Dicom_Viewer.Repositories
{
    public class DicomViewerRepo
    {
        private readonly string _conn;

        public DicomViewerRepo(IConfiguration conn)
        {
            _conn = conn.GetConnectionString("DefaultConnection");
        }


        // Get all studies from the database
        public async Task<IEnumerable<DicomStudyModels>> GelAllStudies()
        {
            using (var connection = new SqlConnection(_conn))
            {
                string query = "SELECT * FROM DicomStudies ORDER BY StudyInstanceUID";

                var result = await connection.QueryAsync<DicomStudyModels>(query);

                return result;
            }

        }


        // Get a study by its StudyInstanceUID

        public async Task<IEnumerable<DicomStudyModels>> GetStudyByUID(string studyInstanceUID)
        {
            using (var connection = new SqlConnection(_conn))
            {
                string query = "SELECT * FROM DicomStudies WHERE StudyInstanceUID = @StudyInstanceUID";
                var result = await connection.QueryAsync<DicomStudyModels>(query, new { StudyInstanceUID = studyInstanceUID });
                return result;
            }

        }

        // Post a new study to the database

        public async Task<int> PostStudy(DicomStudyModels study)
        {
            using (var connection = new SqlConnection(_conn))
            {
                string query = "INSERT INTO DicomStudies (Id, StudyInstanceUID, PatientName, PatientID, Modality, StudyDate, FilePath) VALUES (@Id, @StudyInstanceUID, @PatientName, @PatientID, @Modality, @StudyDate, @FilePath)";
                var result = await connection.ExecuteAsync(query, study);
                return result;
            }
        }

        // delete a study from the database by its StudyInstanceUID

        public async Task<int> DeleteStudy(string studyInstanceUID)
        {
            using (var connection = new SqlConnection(_conn))
            {
                string query = "DELETE FROM DicomStudies WHERE StudyInstanceUID = @StudyInstanceUID";
                var result = await connection.ExecuteAsync(query, new { StudyInstanceUID = studyInstanceUID });
                return result;
            }
        }
    }
}