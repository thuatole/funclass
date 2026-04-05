---
name: Level design - autonomous vs scripted balance
description: When designing game levels, autonomous behaviors and scripted events must be balanced together to prevent early game-over before all scripted events fire
type: feedback
---

Khi tạo level mới, autonomous behaviors + scripted events phải được tính toán cùng nhau. Nếu autonomous disruption quá cao, game fail trước khi scripted events kịp diễn ra.

**Why:** Series 5a test phát hiện FirstDaySummerBreak game fail tại 58s vì autonomous behaviors đẩy disruption lên 70% trước khi events 3-4 (60s, 80s) kịp fire.

**How to apply:** Sau khi tạo level config, luôn chạy scenario test để verify toàn bộ kịch bản diễn ra. Nếu game fail sớm: giảm calmInteractionChance/distractedInteractionChance, tăng maxDisruptionThreshold, hoặc rút ngắn timing giữa scripted events.
