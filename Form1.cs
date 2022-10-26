using SheepHerderAI.AI;
using SheepHerderAI.Configuration;
using SheepHerderTeach.Courses;
using System.Windows.Forms;

namespace SheepHerderAI;

/// <summary>
/// Represents the simple form of "Images" (containing flock+dog) or graphs.
/// </summary>
public partial class Form1 : Form
{
    /// <summary>
    /// Size of each play area (small enough to show lots on a 17" monitor whilst still working effectively.
    /// </summary>
    const int c_width = 300;

    /// <summary>
    /// Size of each play area (small enough to show lots on a 17" monitor whilst still working effectively.
    /// </summary>
    const int c_height = 300;

    /// <summary>
    /// This stores the images (one per neural network id / flock).
    /// </summary>
    readonly PictureBox[] pictureBoxArray;

    /// <summary>
    /// Enable different courses.
    /// </summary>
    readonly ICourse Course = new Course2();

    #region EVENT HANDLERS
    /// <summary>
    /// Constructor.
    /// </summary>
    public Form1()
    {
        InitializeComponent();

        timer1.Interval = 5;
        Width = c_width * 3 + 22;
        Height = c_height + Height - this.RectangleToScreen(this.ClientRectangle).Height + 2; // 44 approx is the title bar.

        List<PictureBox> pictureBoxes = new();

        for (int i = 0; i < Config.NumberOfAIdogs; i++)
        {
            PictureBox pb = new()
            {
                Size = new Size(c_width, c_height),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Margin = new(1),
                Tag = i
            };

            flowLayoutPanel1.Controls.Add(pb);

            pictureBoxes.Add(pb);
        }

        pictureBoxArray = pictureBoxes.ToArray();
        pictureBoxArray[0].MouseMove += User_MouseMove;
    }

    /// <summary>
    /// Animation is frame by frame, initiated each tick.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Timer1_Tick(object? sender, EventArgs e)
    {
        LearnToHerd.Learn();

        List<Bitmap> images = LearnToHerd.DrawAll();

        int i = 0;

        foreach (Bitmap image in images)
        {
            pictureBoxArray[i].Image?.Dispose();
            pictureBoxArray[i].Image = image;
            ++i;
        }
    }

    /// <summary>
    /// On form load, we create the flock, and position fences and score zone.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Form1_Load(object sender, EventArgs e)
    {
        LearnToHerd.s_sizeOfPlayingField = new Size(c_width, c_height);

        Course.DefineSheepPenAndFences(c_width, c_height,
                                       out LearnToHerd.s_sheepPenScoringZone,
                                       out LearnToHerd.s_lines,
                                       out LearnToHerd.s_wayPointsSheepNeedsToGoThru);
        LearnToHerd.StartLearning();

        timer1.Tick += Timer1_Tick;
        timer1.Start();
    }

    #endregion

    /// <summary>
    /// Set the predator based on the mouse position.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void User_MouseMove(object? sender, MouseEventArgs e)
    {
        if (sender is null) return;

        PictureBox pb = (PictureBox)sender;

        if ((int)pb.Tag != 0) return; // only user's picturebox can be clicked on.

        LearnToHerd.s_flock[0].dog.DesiredPosition = new PointF(e.X, e.Y);
    }

    /// <summary>
    /// User can press keys to save/load model, pause/slow, mutate etc.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Form1_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.P:
                timer1.Enabled = !timer1.Enabled;
                break;

            case Keys.F:
                // "F" slow mode
                StepThroughSpeeds();
                break;

            case Keys.S:
                // "S" saves
                NeuralNetwork.Save();
                break;

            case Keys.M:
                LearnToHerd.NextGeneration();
                break;
        }
    }

    /// <summary>
    /// Pressing "S" slows things down, 2x slower, 5x slower, 10x slower, then back to normal speed.
    /// </summary>
    internal void StepThroughSpeeds()
    {
        var newInterval = timer1.Interval switch
        {
            40 => 50,
            50 => 100,
            100 => 1000,
            _ => 40,
        };

        timer1.Interval = newInterval;
    }
}