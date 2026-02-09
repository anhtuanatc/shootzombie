# 🎭 Player Animation Setup Guide

## 📋 Checklist Setup Animation

### Step 1: Import Character Model

1. **Drag model vào Unity**
   - Đặt trong folder `Assets/Models/Player/`
   - Unity sẽ tự động import

2. **Configure Model Import Settings**
   ```
   Select model → Inspector → Model tab:
   
   ✅ Scale Factor: 1 (hoặc adjust cho phù hợp)
   ✅ Mesh Compression: Off (quality cao)
   ✅ Read/Write Enabled: Off (optimize)
   ✅ Optimize Mesh: On
   ✅ Generate Colliders: Off (tạo manual)
   
   Rig tab:
   ✅ Animation Type: Humanoid (nếu là người)
   ✅ Avatar Definition: Create From This Model
   ✅ Optimize Game Objects: On (optional)
   
   Animation tab:
   ✅ Import Animation: On (nếu có animations)
   ✅ Bake Animations: On
   ✅ Resample Curves: On
   
   → Apply
   ```

---

## 🎬 Step 2: Setup Animations

### Option A: Model có sẵn animations

1. **Extract animations:**
   ```
   Model → Animation tab → Clips section
   
   Mỗi animation clip:
   - Name: Idle, Walk, Run, Shoot, etc.
   - Start/End frames
   - Loop Time: ✅ (cho Idle, Walk, Run)
   - Loop Pose: ✅
   
   → Apply
   ```

2. **Extract vào folder riêng:**
   ```
   Create folder: Assets/Animations/Player/
   Drag clips vào folder này
   ```

### Option B: Download animations riêng

1. **Download từ Mixamo/Asset Store**
   - Idle animation
   - Walk animation  
   - Run animation (optional)
   - Shoot animation
   - Death animation
   - Hit/Damage animation (optional)

2. **Import vào Unity:**
   ```
   Drag vào Assets/Animations/Player/
   
   Mỗi animation:
   Rig tab:
   ✅ Animation Type: Humanoid
   ✅ Avatar Definition: Copy From Other Avatar
   ✅ Source: [Your player model's Avatar]
   
   → Apply
   ```

---

## 🎛 Step 3: Create Animator Controller

1. **Create Animator Controller:**
   ```
   Assets/Animations/Player/ → Right-click
   → Create → Animator Controller
   → Name: "PlayerAnimator"
   ```

2. **Open Animator window:**
   ```
   Window → Animation → Animator
   Select PlayerAnimator
   ```

3. **Setup Parameters:**
   ```
   Parameters tab → + button:
   
   ✅ Speed (Float) - Movement speed
   ✅ IsMoving (Bool) - Đang di chuyển?
   ✅ IsShooting (Bool) - Đang bắn?
   ✅ Die (Trigger) - Chết
   ✅ Hit (Trigger) - Bị đánh (optional)
   ```

4. **Create States:**
   ```
   Base Layer:
   
   [Entry] → [Idle]
   
   States cần tạo:
   - Idle (default state - orange)
   - Walk/Move
   - Shoot
   - Death
   - Hit (optional)
   ```

5. **Assign Animation Clips:**
   ```
   Drag animation clips vào states:
   
   Idle state → Drag "Idle" clip
   Walk state → Drag "Walk" clip
   Shoot state → Drag "Shoot" clip
   Death state → Drag "Death" clip
   ```

---

## 🔀 Step 4: Setup Transitions

### Idle ↔ Walk

```
Idle → Walk:
  Conditions: IsMoving = true
  Settings:
    ✅ Has Exit Time: false
    ✅ Transition Duration: 0.1-0.2s
    ✅ Interruption Source: Current State

Walk → Idle:
  Conditions: IsMoving = false
  Settings:
    ✅ Has Exit Time: false
    ✅ Transition Duration: 0.1-0.2s
```

### Any State → Shoot (Optional - nếu muốn shoot override)

```
Any State → Shoot:
  Conditions: IsShooting = true
  Settings:
    ✅ Has Exit Time: false
    ✅ Transition Duration: 0.05s
    
Shoot → Exit:
  Conditions: IsShooting = false
  Settings:
    ✅ Has Exit Time: true
    ✅ Exit Time: 0.8-0.9 (gần hết animation)
    ✅ Transition Duration: 0.1s
```

### Any State → Death

```
Any State → Death:
  Conditions: Die (trigger)
  Settings:
    ✅ Has Exit Time: false
    ✅ Transition Duration: 0.1s
    ✅ Can Transition To Self: false
    
Death state:
  ✅ Write Defaults: false
  ✅ Speed: 1
  (No transitions out - end state)
```

---

## 🎮 Step 5: Setup Player GameObject

1. **Add Animator component:**
   ```
   Player GameObject → Add Component → Animator
   
   Settings:
   ✅ Controller: PlayerAnimator
   ✅ Avatar: [Auto-assigned from model]
   ✅ Apply Root Motion: false (cho top-down shooter)
   ✅ Update Mode: Normal
   ✅ Culling Mode: Always Animate
   ```

2. **Verify hierarchy:**
   ```
   Player (root)
   ├── Model (visual)
   │   ├── Armature/Skeleton
   │   └── Mesh
   ├── Rigidbody
   ├── Collider
   ├── PlayerMovement script
   ├── PlayerShooting script
   └── Animator ← Phải có!
   ```

---

## 💻 Step 6: Update PlayerMovement Script

Code đã có sẵn support animations! Chỉ cần verify:

```csharp
// PlayerMovement.cs đã có:

private Animator _animator;

// Animation parameter hashes (cached for performance)
private static readonly int SpeedHash = Animator.StringToHash("Speed");
private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

private void CacheComponents()
{
    _animator = GetComponent<Animator>();  // ✅ Tự động tìm
}

private void UpdateAnimator()
{
    if (_animator == null) return;
    
    // Update animation parameters
    _animator.SetFloat(SpeedHash, _currentVelocity.magnitude);
    _animator.SetBool(IsMovingHash, IsMoving);
}
```

**Không cần sửa gì!** Script đã ready! ✅

---

## 🎯 Step 7: Test Animations

1. **Play game**
2. **Test từng animation:**

### Test Idle:
```
- Đứng yên
- Expected: Idle animation plays
```

### Test Walk:
```
- Nhấn WASD
- Expected: Walk animation plays
- Release: Back to Idle
```

### Test Shoot (nếu có):
```
- Click chuột
- Expected: Shoot animation plays
- Animation blends với movement
```

### Test Death (nếu có):
```
- Player health = 0
- Expected: Death animation plays
- No transitions out
```

---

## 🔧 Troubleshooting

### Animation không chạy:

**Check 1: Animator component**
```
Player → Inspector → Animator
✅ Controller assigned?
✅ Avatar assigned?
✅ Enabled?
```

**Check 2: Parameters match**
```
Animator Controller parameters:
✅ Speed (Float)
✅ IsMoving (Bool)

Code uses:
✅ Animator.StringToHash("Speed")
✅ Animator.StringToHash("IsMoving")

→ Names phải GIỐNG NHAU!
```

**Check 3: Transitions**
```
Animator window:
✅ Transitions exist?
✅ Conditions correct?
✅ Has Exit Time = false (cho responsive transitions)
```

### Animation bị giật:

**Fix 1: Transition duration**
```
Transitions:
✅ Duration: 0.1-0.2s (không quá dài)
✅ Offset: 0
```

**Fix 2: Animation quality**
```
Animation Import Settings:
✅ Resample Curves: On
✅ Anim. Compression: Off
```

### Character xoay sai hướng:

**Fix: Root motion**
```
Animator component:
✅ Apply Root Motion: false

PlayerMovement handles rotation!
```

### Animation loop không smooth:

**Fix: Loop settings**
```
Animation clip:
✅ Loop Time: On
✅ Loop Pose: On
✅ Cycle Offset: 0
```

---

## 📝 Animation Parameters Reference

### Parameters PlayerMovement sử dụng:

| Parameter | Type | Purpose | Set by |
|-----------|------|---------|--------|
| `Speed` | Float | Movement speed magnitude | `UpdateAnimator()` |
| `IsMoving` | Bool | Is player moving? | `UpdateAnimator()` |

### Parameters optional (có thể thêm):

| Parameter | Type | Purpose | Set by |
|-----------|------|---------|--------|
| `IsShooting` | Bool | Is shooting? | `PlayerShooting` |
| `Die` | Trigger | Death trigger | `PlayerHealth` |
| `Hit` | Trigger | Damage taken | `PlayerHealth` |
| `IsGrounded` | Bool | On ground? | `PlayerMovement` |

---

## 🎨 Advanced: Blend Trees (Optional)

Nếu muốn smooth transition giữa Idle/Walk/Run:

1. **Create Blend Tree:**
   ```
   Animator → Right-click → Create State → From New Blend Tree
   Name: "Movement"
   ```

2. **Setup Blend Tree:**
   ```
   Double-click Movement state
   
   Blend Type: 1D
   Parameter: Speed
   
   Motions:
   - Speed 0.0: Idle
   - Speed 2.5: Walk
   - Speed 5.0: Run (optional)
   
   ✅ Automate Thresholds
   ```

3. **Update transitions:**
   ```
   Entry → Movement (default)
   
   No Idle/Walk states needed!
   Blend tree handles it automatically
   ```

---

## 🎯 Quick Setup Checklist

- [ ] Import character model
- [ ] Configure import settings (Humanoid rig)
- [ ] Import/extract animations
- [ ] Create Animator Controller
- [ ] Add parameters: Speed, IsMoving
- [ ] Create states: Idle, Walk
- [ ] Assign animation clips
- [ ] Setup transitions
- [ ] Add Animator component to Player
- [ ] Assign controller
- [ ] Test in Play mode

---

## 📚 Recommended Animations

### Minimum (Basic shooter):
- ✅ Idle
- ✅ Walk/Run
- ✅ Shoot (optional - có thể dùng upper body layer)

### Recommended (Polished):
- ✅ Idle
- ✅ Walk
- ✅ Run
- ✅ Shoot (Idle)
- ✅ Shoot (Walk)
- ✅ Death
- ✅ Hit/Damage

### Advanced (Full featured):
- All above +
- ✅ Reload
- ✅ Jump (nếu có)
- ✅ Crouch (nếu có)
- ✅ Melee attack
- ✅ Victory pose

---

## 🌐 Resources

### Free Animation Sources:
- **Mixamo** - mixamo.com (best for humanoid)
- **Unity Asset Store** - Free animation packs
- **Sketchfab** - Some free rigged characters

### Tips for Mixamo:
1. Upload your character (without animations)
2. Auto-rig (if not rigged)
3. Download animations:
   - Format: FBX for Unity
   - Skin: Without Skin (animations only)
   - FPS: 30
   - Keyframe Reduction: None

---

## ✅ Final Check

Animation setup hoàn chỉnh khi:
- [ ] Character model imported correctly
- [ ] Rig type = Humanoid
- [ ] Animator Controller created
- [ ] Parameters match code
- [ ] States và transitions setup
- [ ] Animator component on Player
- [ ] Animations play smoothly
- [ ] Transitions responsive
- [ ] No errors in Console

**Bây giờ player của bạn sẽ có animations đẹp!** 🎭✨

---

## 💡 Pro Tips

1. **Cache Animator hashes** - PlayerMovement đã làm rồi! ✅
2. **Use Blend Trees** cho smooth speed transitions
3. **Layer animations** - Upper body shoot, lower body walk
4. **IK (Inverse Kinematics)** - Aim at mouse position
5. **Root Motion** - Off cho top-down, On cho 3rd person

Nếu cần help với bất kỳ step nào, hãy hỏi tôi! 🚀
