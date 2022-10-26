using SheepHerderAI;

namespace SheepHerderTeach.Courses;

internal class Course1 : ICourse
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
        scoringZone = new Rectangle(courseWidth - (courseWidth / 7), 0, courseWidth / 7, courseHeight / 7);

        fences = new();

        // the pen
        List<PointF> lines = new()
        {
            new PointF(scoringZone.Right-1, scoringZone.Height),
            new PointF(scoringZone.Right-1, 0),
            new PointF(scoringZone.Left, 0),
            new PointF(scoringZone.Left, scoringZone.Height),
            new PointF(scoringZone.Left-scoringZone.Width/2, scoringZone.Height+scoringZone.Height/2)
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
            new PointF(courseWidth/ 4, courseHeight/ 4 * 3)
        };

        fences.Add(lines.ToArray());

        // a restricted point
        lines = new()
        {
            new PointF(courseWidth / 2, 0),
            new PointF(courseWidth / 2, courseHeight/ 4 *1.8f),
            new PointF(courseWidth/2 + courseWidth / 4, courseHeight / 4 * 1.8f)
        };

        fences.Add(lines.ToArray());

        lines = new()
        {
            new PointF(courseWidth / 2, courseHeight),
            new PointF(courseWidth / 2, courseHeight - courseHeight/ 4 * 1.8f)
        };

        fences.Add(lines.ToArray());

        // these are "way points" that the sheep must go thru, that the AI must try to make happen
        // Points were based on 674 x 500, so we need to scale them here.
        waypoints= new Point[] {
                                // point 0 must be lower than all first sheep
                               // ScalePoint(83, 113),  //0
                               // ScalePoint(83, 143),  //1
                                ScalePoint(81, 192),  //2
                              //  ScalePoint(79, 241),  //3
                              //  ScalePoint(78, 281),  //4
                                ScalePoint(78, 321),  //5
                              //  ScalePoint(87, 357),  //6                                    
                              //  ScalePoint(96, 393),  //7
                                ScalePoint(155, 427), //8
                              //  ScalePoint(165, 434), //9
                              //  ScalePoint(200, 415), //10
                              //  ScalePoint(230, 391), //11
                              //  ScalePoint(251, 368), //12
                              //  ScalePoint(271, 322), //13
                                ScalePoint(310, 260), //14
                                ScalePoint(336, 260), //15
                              //  ScalePoint(364, 255), //16
                              //  ScalePoint(410, 249), //17
                              //  ScalePoint(463, 249), //18
                              //  ScalePoint(517, 250), //19
                                ScalePoint(555, 260), //20
                              //  ScalePoint(570, 207), //21
                              //  ScalePoint(586, 178), //22
                                ScalePoint(602, 146), //23
                              //  ScalePoint(612, 93),  //24
                                ScalePoint(619, 40)   //25
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