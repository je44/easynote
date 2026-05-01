using System.Windows.Media;

namespace EasyNote;

public partial class MainWindow
{
    private void ApplyThemePalette()
    {
        if (IsNightTheme)
        {
            SetBrush("ChromeBackgroundBrush", "#242A22");
            SetBrush("ChromeBorderBrush", "#33465240");
            SetBrush("SurfaceBrush", "#30382D");
            SetBrush("CardSelectedSurfaceBrush", "#283022");
            SetBrush("PinnedHoverOverlayBrush", "#6A5D28");
            SetBrush("SurfaceStrongBrush", "#3A4435");
            SetBrush("EditSurfaceBrush", "#57664A");
            SetBrush("EditStatusBrush", "#D8C46C");
            SetBrush("SecondaryActionBrush", "#3A4435");
            SetBrush("SecondaryActionHoverBrush", "#46523F");
            SetBrush("SecondaryActionPressedBrush", "#20261F");
            SetBrush("SurfaceHoverBrush", "#46523F");
            SetBrush("SurfacePressedBrush", "#20261F");
            SetBrush("TextPrimaryBrush", "#EEF1E5");
            SetBrush("TextMutedBrush", "#BBC5AD");
            SetBrush("TextSubtleBrush", "#8D9980");
            SetBrush("NoteTextBrush", "#EEF1E5");
            SetBrush("NoteTextSubtleBrush", "#AEB89F");
            SetBrush("AccentBrush", "#D8C46C");
            SetBrush("AccentHoverBrush", "#E3D17D");
            SetBrush("AccentPressedBrush", "#BCA84F");
            SetBrush("HeaderIconHoverBrush", "#F0D76E");
            SetBrush("HeaderIconPressedBrush", "#FFE58A");
            SetBrush("PinnedIconBrush", "#F0D76E");
            SetBrush("PinnedSurfaceBrush", "#4A4325");
            SetBrush("PinnedBorderBrush", "#A9923E");
            SetBrush("DangerBrush", "#E28B7F");
            SetBrush("DoneCornerBrush", "#D7473F");
            SetBrush("OverlayBrush", "#F23F3328");
            SetBrush("ScrollTrackBrush", "#3346503E");
            SetBrush("ScrollThumbBrush", "#8895A083");
            return;
        }

        SetBrush("ChromeBackgroundBrush", "#ECE4D5");
        SetBrush("ChromeBorderBrush", "#40D8CEB8");
        SetBrush("SurfaceBrush", "#DED5C3");
        SetBrush("CardSelectedSurfaceBrush", "#D3CAB8");
        SetBrush("PinnedHoverOverlayBrush", "#E1CB7D");
        SetBrush("SurfaceStrongBrush", "#F4ECDC");
        SetBrush("EditSurfaceBrush", "#DEC985");
        SetBrush("EditStatusBrush", "#5F7D5C");
        SetBrush("SecondaryActionBrush", "#F4ECDC");
        SetBrush("SecondaryActionHoverBrush", "#E8DEC9");
        SetBrush("SecondaryActionPressedBrush", "#D7CCB7");
        SetBrush("SurfaceHoverBrush", "#E8DEC9");
        SetBrush("SurfacePressedBrush", "#D7CCB7");
        SetBrush("TextPrimaryBrush", "#2F291E");
        SetBrush("TextMutedBrush", "#756B5C");
        SetBrush("TextSubtleBrush", "#928674");
        SetBrush("NoteTextBrush", "#2F291E");
        SetBrush("NoteTextSubtleBrush", "#756B5C");
        SetBrush("AccentBrush", "#668564");
        SetBrush("AccentHoverBrush", "#73916E");
        SetBrush("AccentPressedBrush", "#557252");
        SetBrush("HeaderIconHoverBrush", "#557252");
        SetBrush("HeaderIconPressedBrush", "#3F5D3E");
        SetBrush("PinnedIconBrush", "#536E50");
        SetBrush("PinnedSurfaceBrush", "#E9D89D");
        SetBrush("PinnedBorderBrush", "#B59B3E");
        SetBrush("DangerBrush", "#B45C50");
        SetBrush("DoneCornerBrush", "#C9342E");
        SetBrush("OverlayBrush", "#F4E0CFC2");
        SetBrush("ScrollTrackBrush", "#2A2F291E");
        SetBrush("ScrollThumbBrush", "#7A756B5C");
    }

    private void SetBrush(string key, string color)
    {
        Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
}
