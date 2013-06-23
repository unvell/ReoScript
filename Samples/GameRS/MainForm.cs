using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unvell.ReoScript;
using System.IO;
using GameRS.Properties;
using System.Text.RegularExpressions;

namespace GameRS
{
	public partial class MainForm : Form
	{
		private static readonly int FPS = 40;

		private Timer timerRun = new Timer();
		private Timer timerRedrawFrame = new Timer();

		internal ScriptRunningMachine srm = new ScriptRunningMachine();

		public MainForm()
		{
			InitializeComponent();

			// enable DirectAccess
			srm.WorkMode |= MachineWorkMode.AllowDirectAccess;

			float interval = 1000f / FPS;

			timerRun.Interval = (int)(interval);
			timerRun.Tick += (s, e) =>
			{
				srm.InvokeFunctionIfExisted("run", null);
				labFps.Text = string.Format("Current FPS: " + battleground.CurrentFps);
			};

			timerRedrawFrame.Interval = (int)(interval/2);
			timerRedrawFrame.Tick += (s, e) =>
			{
				battleground.Invalidate();
				battleground.Update();
			};

			using (StreamReader sr = new StreamReader(new MemoryStream(Resources.main)))
			{
				editor.Text = sr.ReadToEnd();
			}

			battleground.Srm = this.srm;
		}

		private Sprite sprite1 = new Sprite() { X = 100, Y = 100, Width = 30, Height = 30 };

		private void btnStart_Click(object sender, EventArgs e)
		{
			if (timerRun.Enabled)
			{
				timerRun.Enabled = false;
				btnStart.Text = "&Start";
			}
			else
			{
				Start();

				timerRun.Enabled = true;
				btnStart.Text = "&Stop";
			}

			timerRedrawFrame.Enabled = timerRun.Enabled;
		}

		public void Start()
		{
			// remove old sprites
			battleground.Sprites.Clear();

			// clear script context, all of global variables will be removed
			srm.Reset();

			// extend function to create .net Color object
			srm["rgb"] = new NativeFunctionObject("rgb", (ctx, owner, args) =>
			{
				int r = ScriptRunningMachine.GetIntParam(args, 0, 0);
				int g = ScriptRunningMachine.GetIntParam(args, 1, 0);
				int b = ScriptRunningMachine.GetIntParam(args, 2, 0);

				return Color.FromArgb(r, g, b);
			});

			// add battleground object
			srm["battleground"] = battleground;
			
			// run script
			srm.Run(editor.Text);
		}

	}

	interface ISprite
	{
		void Draw(Graphics g);
	}

	class Sprite : ISprite
	{
		public int X { get; set; }
		public int Y { get; set; }

		public int Width { get; set; }
		public int Height { get; set; }

		public Color Color { get; set; }

		public void Draw(Graphics g)
		{
			Rectangle rect = new Rectangle(X, Y, Width, Height);

			if (Color.A == 0 || Color.IsEmpty)
			{
				g.FillRectangle(Brushes.Black, rect);
			}
			else
			{
				using (Brush b = new SolidBrush(Color))
				{
					g.FillRectangle(b, rect);
				}
			}
		}

	}

	class Battleground : Panel
	{
		public ScriptRunningMachine Srm { get; set; }

		public Battleground()
		{
			DoubleBuffered = true;
		}

		public new int Width { get { return ClientRectangle.Width; } }
		public new int Height { get { return ClientRectangle.Height; } }

		private List<ISprite> sprites = new List<ISprite>();

		internal List<ISprite> Sprites
		{
			get { return sprites; }
			set { sprites = value; }
		}

		private int lastFps = 0;

		public int CurrentFps { get; set; }

		private int lastSecond = 0;

		protected override void OnPaint(PaintEventArgs e)
		{
			foreach (ISprite sprite in sprites)
			{
				sprite.Draw(e.Graphics);
			}

			if (lastSecond != DateTime.Now.Second)
			{
				CurrentFps = lastFps;
				lastFps = 0;
				lastSecond = DateTime.Now.Second;
			}

			lastFps++;
		}

		public Sprite NewSprite()
		{
			Sprite newSprite = new Sprite() { Width = 30, Height = 30 };
			sprites.Add(newSprite);
			return newSprite;
		}
	}
}
