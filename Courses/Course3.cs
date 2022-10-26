using SheepHerderAI;

namespace SheepHerderTeach.Courses;

internal class Course3 : ICourse
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
        scoringZone = new Rectangle(141,0,46,30);

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
            new PointF(50, 2),
            new PointF(50, 92),
        };

        fences.Add(lines.ToArray());

        lines = new()
        {
            new PointF(51,138),
            new PointF(101,138),
            new PointF(101,49)
        };

        fences.Add(lines.ToArray());

        lines = new()
        {
            new PointF(2,177),
            new PointF(51,177),
            new PointF(51,115)
        };

        fences.Add(lines.ToArray());

        lines = new()
        {
            new PointF(101,0),
            new PointF(101,18),
        };

        fences.Add(lines.ToArray());


        lines = new()
        {
            new PointF(141,0),
            new PointF(141,178),
            new PointF(96,178),
            new PointF(96,226),
            new PointF(239,226) //a
        };

        fences.Add(lines.ToArray());

        lines = new()
        {
            new PointF(96,300),
            new PointF(96,256)
        };

        fences.Add(lines.ToArray());

        lines = new()
        {
            new PointF(149,226),
            new PointF(149,270)
        };

        fences.Add(lines.ToArray());


        lines = new()
        {
            new PointF(199,255),
            new PointF(199,299)
        };

        fences.Add(lines.ToArray());

        lines = new()
        {
            new PointF(239,270),
            new PointF(239,30)
        };

        fences.Add(lines.ToArray());

        lines = new()
        {
            new PointF(267,30),
            new PointF(221,30),
            new PointF(168,104),
            new PointF(168,180)
        };

        fences.Add(lines.ToArray());

        // these are "way points" that the sheep must go thru, that the AI must try to make happen
        waypoints = new Point[] {
                                // point 0 must be lower than all first sheep
                                new Point(24,104), // 1
                                new Point(73,104), // 2
                                new Point(101,31), // 3
                                new Point(122,157), // 4
                                new Point(68,162), // 5
                                new Point(97,242), // 6
                                new Point(150,282), // 7
                                new Point(207,236), // 8
                                new Point(241,279), // 9
                                new Point(278,279), // 10
                                new Point(278,18), // 11
                                new Point(205,18), // 12
                                new Point(177,48), // 13
                                new Point(161,17), // 14

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