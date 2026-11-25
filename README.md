# Đua Xe Địa Hình 3D

Game đua xe off-road 3D góc nhìn thứ ba, xây dựng trên **Unity 6000.6.7f1** (Built-in Render Pipeline) và **C#**. Toàn bộ địa hình, xe, tay đua, vật liệu và âm thanh đều được sinh bằng mã nguồn, không phụ thuộc tài nguyên 3D bên ngoài.

![Logo](Logo/Logo.png)

## Chức năng chính

- Luồng chơi khép kín 6 màn hình: Menu → Chọn xe → Chọn tay đua → Chọn địa hình → Đua → Kết quả.
- 3 mẫu xe (Jeep, Buggy, Monster Truck) với thông số và ngoại hình khác nhau.
- 3 tay đua với màu áo riêng.
- 4 loại địa hình: Sa mạc, Rừng, Đồi núi, Đồng băng — khác nhau về màu đất, sương mù, độ bám và đạo cụ.
- Mô phỏng vật lý bằng Rigidbody + WheelCollider: tăng tốc, phanh, lái, nitro, chống lật, tự dựng xe.
- 2 đối thủ AI bám tim đường, tự phục hồi khi kẹt hoặc lật.
- HUD hiển thị tốc độ, thời gian, thứ hạng, thanh nitro; đếm ngược 3–2–1 trước khi xuất phát.
- Lưu lựa chọn và kết quả bằng PlayerPrefs.
- Công cụ Editor tự sinh scene, render ảnh xem trước và đóng gói game.

## Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Game engine | Unity 6000.6.7f1 |
| Ngôn ngữ | C# |
| Vật lý | Rigidbody, WheelCollider, Terrain Collider |
| Giao diện | uGUI (Canvas, Text, Button, Image) |
| Lưu trữ | PlayerPrefs |
| Đồ họa | Mesh sinh thủ tục, Terrain, material Standard |
| Âm thanh | AudioClip sinh bằng sóng sin |

## Cấu trúc thư mục

```
Assets/
  Editor/        GameBuilder, PreviewRenderer, RacingSetup, BuildTools
  Scenes/        6 scene từ 01_MenuChinh đến 06_KetQua
  Scripts/
    Core/        GameData, InputHelper, MatHelper
    UI/          MenuController, SelectionMenu, ResultController, UIHelper
    Car/         CarController, CarFactory, CarMesh, AICarController, CameraFollow
    Race/        RaceManager, TerrainGenerator, FinishLine
    Audio/       AudioFactory
  Resources/     Ảnh xem trước xe, tay đua, địa hình
  Art/           Logo.png, Icon.png, Icon.ico
Logo/            Bộ logo dùng để nộp bài
```

## Cách chạy

### Chạy bản đã đóng gói

Tải tệp ZIP trong mục **Releases**, giải nén và chạy `DuaXeDiaHinh3D.exe`. Yêu cầu Windows 10/11 64-bit.

### Mở bằng Unity

1. Cài Unity Hub và Unity **6000.6.7f1**.
2. Mở project tại thư mục gốc của kho lưu trữ.
3. Mở scene `Assets/Scenes/01_MenuChinh.unity` và bấm **Play**.

### Tự đóng gói

Trong Unity chọn:

- **Game → Build Windows x64**
- **Game → Build Android APK** (cần cài Android Build Support)
- **Game → Build Android AAB**

## Điều khiển

| Phím | Chức năng |
|---|---|
| W / S | Tiến / lùi |
| A / D | Lái trái / phải |
| Space | Phanh |
| Shift | Nitro |
| R | Dựng xe khi bị lật |
| Esc | Về menu / thoát |

## Logo

| Tệp | Kích thước | Mục đích |
|---|---|---|
| `Logo/Logo.png` | 2200×700 | Logo ngang, nền trong suốt |
| `Logo/Icon.png` | 1024×1024 | Biểu tượng ứng dụng |
| `Logo/Icon.ico` | 16–256 px | Icon cho tệp .exe Windows |

## Tài liệu

Báo cáo phân tích và thiết kế hệ thống: `PhanTichThietKe_GameDuaXeDiaHinh3D.docx`.

## Tác giả

- Sinh viên: ..............................
- Lớp: ..............................
- Giảng viên hướng dẫn: ..............................
