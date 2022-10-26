using SheepHerderAI;

namespace SheepHerderTeach.Courses;

internal class Course2 : ICourse
{
    /// <summary>
    /// Definition of a basic sheep course.
    /// </summary>
    /// <param name="courseWidth"></param>
    /// <param name="courseHeight"></param>
    /// <param name="scoringZone">(out) Area you need to herd the sheep into.</param>
    /// <param name="fences">(out) List of fences. Note: sheep can jump them if pushed hard enough.</param>
    /// <param name="waypoints">(out) List of points the dog should aim to push the sheep.</param>
    public void DefineSheepPenAndFences(int courseWidth, int courseHeight, out RectangleF scoringZone, out List<PointF[]> fences, out Point[] waypoints)
    {
        scoringZone = new Rectangle(courseWidth / 2 - (courseWidth / 14), courseHeight / 2 - (courseHeight / 14), courseWidth / 7, courseHeight / 7);

        fences = new();

        // the pen
        List<PointF> lines = new()
        {
            new PointF(scoringZone.Left-1, scoringZone.Bottom),
            new PointF(scoringZone.Left-1, scoringZone.Top),
            new PointF(scoringZone.Right, scoringZone.Top),
            new PointF(scoringZone.Right, scoringZone.Bottom)
        };

        fences.Add(lines.ToArray());

        // all the way around the screen
        lines = new()
        {
            new PointF(2, 2),
            new PointF(courseWidth-3, 2),
            new PointF(courseWidth-3, courseHeight-3),
            new PointF(2, courseHeight-3),
            new PointF(2, 2)
        };

        fences.Add(lines.ToArray());

        // the start
        lines = new()
        {
            new PointF(courseWidth / 4, 0),
            new PointF(courseWidth/ 4, courseHeight/ 4 * 3),
            new PointF(courseWidth/ 4*3, courseHeight/ 4 * 3),
            new PointF(courseWidth/ 4*3, courseHeight/ 4),
            new PointF(courseWidth/ 4*2, courseHeight/ 4)
        };

        fences.Add(lines.ToArray());

        lines = new()
        {
            new PointF(courseWidth / 4, scoringZone.Top + scoringZone.Height / 2),
            new PointF(scoringZone.Left, scoringZone.Top + scoringZone.Height / 2),
        };

        fences.Add(lines.ToArray());

        // these are "way points" that the sheep must go thru, that the AI must try to make happen
        // Points were based on 674 x 500, so we need to scale them here.
        waypoints = new Point[] {
                                // point 0 must be lower than all first sheep
                                ScalePoint(81, 192),  //1
                                ScalePoint(81, 427),  //2
                                ScalePoint(600, 427), //3
                                ScalePoint(600, 50),  //4
                                ScalePoint(200, 50),  //5
                                ScalePoint(200, 180), //23
                                ScalePoint(430, 180),   //25
                                ScalePoint(430, 350),
                                new Point((int) (scoringZone.Left+scoringZone.Width/2),(int) (scoringZone.Top+scoringZone.Height/2))
        };
    }

    /// <summary>
    /// The points were based on a 674x500 grid. This makes them proportionate to the size of the picturebox.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private static Point ScalePoint(int x, int y)
    {
        return new Point((int)((float)x / 674f * LearnToHerd.s_sizeOfPlayingField.Width),
                         (int)((float)y / 500f * LearnToHerd.s_sizeOfPlayingField.Height));
    }
}