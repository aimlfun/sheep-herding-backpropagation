using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheepHerderTeach.Courses
{
    /// <summary>
    /// All courses must satisfy this method: the classes define the size of the course, the scoring zone, fences and waypoints.
    /// </summary>
    internal interface ICourse
    {
        /// <summary>
        /// Definition of a course in which sheep are herded. Start is assumed to be top left.
        /// </summary>
        /// <param name="courseWidth">Width of the course.</param>
        /// <param name="courseHeight">Height of the course.</param>
        /// <param name="scoringZone">(out) Area you need to herd the sheep into.</param>
        /// <param name="fences">(out) List of fences. Note: sheep can jump them if pushed hard enough.</param>
        /// <param name="waypoints">(out) List of points the dog should aim to push the sheep.</param>
        public void DefineSheepPenAndFences(int courseWidth, int courseHeight, out RectangleF scoringZone, out List<PointF[]> fences, out Point[] waypoints);
    }
}
