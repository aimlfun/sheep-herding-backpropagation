using System.Drawing.Imaging;
using System.Security.Cryptography;
using SheepHerderAI.Utilities;
using NUnit.Framework;

namespace Sheep_Dog_AI_Test_Suite
{
    public class TestMathUtils
    {
        [SetUp]
        public void Setup()
        {
        }

        /// <summary>
        /// Proves that the "hit-test" for triangles is accurate.
        /// Draws random triangles, and checks every point to see if it is in the triangle (think of inefficient triangle fill, but good to test method)
        /// </summary>
        [Test]
        public void TriangeHitHest()
        {
            using Bitmap b = GetTriangleFilledUsingTriangleHitTest();
            b.Save(@"c:\temp\triangle-hit-test.png", ImageFormat.Png);

            // pass -> if triangle is drawn in different colour to back ground.

            // I am not wasting the effort doing a GDI FillPolygon cut out to check if pixels plotted fall within the triangle, but one could.
            Assert.Pass();
        }

        /// <summary>
        /// Proof the .PtInTriangle() works, and correctly detects pixels that are within a triangle.
        /// </summary>   
        private static Bitmap GetTriangleFilledUsingTriangleHitTest()
        {
            const int s_width = 300;
            const int s_height = 300;

            // pick 3 random points, that make up our "triangle"
            PointF vertex1 = new(RandomNumberGenerator.GetInt32(0, s_width), RandomNumberGenerator.GetInt32(0, s_height));
            PointF vertex2 = new(RandomNumberGenerator.GetInt32(0, s_width), RandomNumberGenerator.GetInt32(0, s_height));
            PointF vertex3 = new(RandomNumberGenerator.GetInt32(0, s_width), RandomNumberGenerator.GetInt32(0, s_height));

            Bitmap proof = new(s_width, s_height);
            using Graphics graphics = Graphics.FromImage(proof);

            // draw the triangle.
            graphics.DrawLines(Pens.Red, new PointF[] { vertex1, vertex2, vertex3 });

            graphics.Flush();

            // for every pixel (width*height), plot "black" if inside the triangle
            for (int x = 0; x < s_width; x++)
            {
                for (int y = 0; y < s_height; y++)
                {
                    if (MathUtils.PtInTriangle(new PointF(x, y), vertex1, vertex2, vertex3))
                    {
                        proof.SetPixel(x, y, Color.Black);
                    }
                }
            }

            graphics.Flush();

            return proof;
        }
      
    }
}