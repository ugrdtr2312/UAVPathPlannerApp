namespace WebApplication.Models.Solver
{
    public class ProblemData
    {
        public List<PointModel> Points { get; set; }

        public List<BaseModel> Bases { get; set; }

        public int MaxFlightTime { get; set; }
        
        public int ChargeTime { get; set; }
        
        public int Speed { get; set; }

        public string AlgorithmType { get; set; }

        public IFormFile? File { get; set; }
    }
}
