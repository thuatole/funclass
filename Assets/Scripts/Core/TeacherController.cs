using UnityEngine;

namespace FunClass.Core
{
    [RequireComponent(typeof(CharacterController))]
    public class TeacherController : MonoBehaviour
    {
        public static TeacherController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Camera")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 80f;

        [Header("Item Holding")]
        [SerializeField] private Transform itemHoldPoint;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionLayer = ~0;

        [Header("Auto Escort")]
        [SerializeField] private bool autoEscortEnabled = false; // Disabled - player must manually interact
        [SerializeField] private float escortCheckInterval = 2f;
        private float lastEscortCheckTime;

        private CharacterController characterController;
        private HeldItem currentHeldItem;
        private float verticalVelocity;
        private float cameraPitch;
        private bool isActive = false;
        private InteractableObject currentLookTarget;
        private StudentAgent currentStudentTarget;
        private MessObject currentMessTarget;

        public Camera PlayerCamera => playerCamera;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[TeacherController] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            characterController = GetComponent<CharacterController>();
            
            if (characterController == null)
            {
                Debug.LogError("[TeacherController] CharacterController component missing!");
            }
            else
            {
                Debug.Log("[TeacherController] CharacterController found");
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                Debug.Log($"[TeacherController] Camera assigned from Camera.main: {playerCamera != null}");
            }

            if (cameraTransform == null && playerCamera != null)
            {
                cameraTransform = playerCamera.transform;
            }
            
            Debug.Log($"[TeacherController] Awake complete - Camera: {playerCamera != null}, CameraTransform: {cameraTransform != null}");
        }

        void OnEnable()
        {
            Debug.Log("[TeacherController] OnEnable called");
            
            if (GameStateManager.Instance != null)
            {
                Debug.Log("[TeacherController] GameStateManager.Instance found, subscribing to OnStateChanged");
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                Debug.Log($"[TeacherController] ✅ Subscribed to OnStateChanged, Current state: {GameStateManager.Instance.CurrentState}");
            }
            else
            {
                Debug.LogWarning("[TeacherController] ⚠️ GameStateManager.Instance is NULL in OnEnable - will retry in Start()");
            }
        }

        void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
        }

        void Start()
        {
            Debug.Log("[TeacherController] Start called");
            
            // Fallback: Subscribe if OnEnable failed (GameStateManager didn't exist yet)
            if (GameStateManager.Instance != null)
            {
                Debug.Log($"[TeacherController] Current game state: {GameStateManager.Instance.CurrentState}");
                
                // Always try to subscribe (duplicate subscriptions are handled by C# events)
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                Debug.Log("[TeacherController] Subscribed to OnStateChanged in Start()");
                
                // If already in InLevel, activate immediately
                if (GameStateManager.Instance.CurrentState == GameState.InLevel)
                {
                    Debug.Log("[TeacherController] Already in InLevel, activating teacher");
                    ActivateTeacher();
                }
            }
            else
            {
                Debug.LogError("[TeacherController] GameStateManager.Instance is STILL null in Start()!");
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (!isActive) return;

            HandleMovement();
            HandleCamera();
            HandleInteractionDetection();
            HandleInteractionInput();
            
            // Auto-escort students who are outside
            if (autoEscortEnabled)
            {
                CheckAndEscortOutsideStudents();
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            Debug.Log($"[TeacherController] HandleGameStateChanged called: {oldState} -> {newState}");
            
            if (newState == GameState.InLevel)
            {
                Debug.Log("[TeacherController] State is InLevel, activating teacher");
                ActivateTeacher();
            }
            else
            {
                Debug.Log($"[TeacherController] State is {newState}, deactivating teacher");
                DeactivateTeacher();
            }
        }

        private void ActivateTeacher()
        {
            isActive = true;
            enabled = true;
            Debug.Log($"[TeacherController] ✅ Teacher ACTIVATED - isActive: {isActive}, enabled: {enabled}");
        }

        private void DeactivateTeacher()
        {
            isActive = false;
            Debug.Log($"[TeacherController] ❌ Teacher DEACTIVATED - isActive: {isActive}");
            Debug.LogWarning($"[TeacherController] DEACTIVATION STACK TRACE:", this);
            enabled = false;
            Debug.Log("[TeacherController] Teacher deactivated");
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
            moveDirection.Normalize();

            Vector3 move = moveDirection * moveSpeed;

            if (characterController.isGrounded)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            move.y = verticalVelocity;

            characterController.Move(move * Time.deltaTime);
        }

        private void HandleCamera()
        {
            if (cameraTransform == null) return;

            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        public void HoldItem(HeldItem item)
        {
            if (currentHeldItem != null)
            {
                DropItem();
            }

            currentHeldItem = item;

            if (itemHoldPoint != null && item != null)
            {
                item.transform.SetParent(itemHoldPoint);
                item.transform.localPosition = Vector3.zero;
                item.transform.localRotation = Quaternion.identity;
            }

            Debug.Log($"[TeacherController] Now holding: {item?.itemName ?? "nothing"}");
        }

        public void DropItem()
        {
            if (currentHeldItem == null) return;

            Debug.Log($"[TeacherController] Dropped: {currentHeldItem.itemName}");

            currentHeldItem.transform.SetParent(null);
            currentHeldItem = null;
        }

        public HeldItem GetHeldItem()
        {
            return currentHeldItem;
        }

        public bool IsHoldingItem()
        {
            return currentHeldItem != null;
        }

        private void HandleInteractionDetection()
        {
            if (cameraTransform == null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionRange, interactionLayer))
            {
                StudentAgent student = hit.collider.GetComponentInParent<StudentAgent>();
                
                if (student != null)
                {
                    if (student != currentStudentTarget)
                    {
                        currentStudentTarget = student;
                        currentLookTarget = null;
                        
                        string prompt = GetContextualPrompt(student);
                        Debug.Log($"[TeacherController] Looking at: {prompt}");
                    }
                }
                else
                {
                    // Check for mess objects first
                    MessObject mess = hit.collider.GetComponent<MessObject>();
                    if (mess != null)
                    {
                        if (mess != currentMessTarget)
                        {
                            currentMessTarget = mess;
                            currentLookTarget = null;
                            currentStudentTarget = null;
                            
                            Debug.Log($"[TeacherController] Looking at: {currentMessTarget.GetInteractionPrompt()}");
                        }
                    }
                    else
                    {
                        InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
                        
                        if (interactable != currentLookTarget)
                        {
                            currentLookTarget = interactable;
                            currentStudentTarget = null;
                            currentMessTarget = null;
                            
                            if (currentLookTarget != null)
                            {
                                Debug.Log($"[TeacherController] Looking at: {currentLookTarget.GetInteractionPrompt()}");
                            }
                        }
                    }
                }
            }
            else
            {
                if (currentLookTarget != null)
                {
                    currentLookTarget = null;
                }
                if (currentStudentTarget != null)
                {
                    currentStudentTarget = null;
                }
                if (currentMessTarget != null)
                {
                    currentMessTarget = null;
                }
            }
        }

        private void HandleInteractionInput()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (currentMessTarget != null)
                {
                    CleanMess(currentMessTarget);
                }
                else if (currentStudentTarget != null)
                {
                    HandleContextualInteraction(currentStudentTarget);
                }
                else if (currentLookTarget != null)
                {
                    currentLookTarget.Interact(this);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.Calm);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.SendToSeat);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.Stop);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                if (currentStudentTarget != null && currentHeldItem != null)
                {
                    UseItemOnStudent(currentStudentTarget, currentHeldItem.gameObject);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.Scold);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.Talk);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.Praise);
                }
            }
        }

        public InteractableObject GetCurrentLookTarget()
        {
            return currentLookTarget;
        }

        public StudentAgent GetCurrentStudentTarget()
        {
            return currentStudentTarget;
        }

        private void InteractWithStudent(StudentAgent student)
        {
            if (student == null) return;

            student.InteractWithTeacher(this);

            TeacherActionType action = student.CurrentState switch
            {
                StudentState.Calm => TeacherActionType.Talk,
                StudentState.Distracted => TeacherActionType.Calm,
                StudentState.ActingOut => TeacherActionType.Stop,
                StudentState.Critical => TeacherActionType.SendToSeat,
                _ => TeacherActionType.Talk
            };

            student.HandleTeacherAction(action);
        }

        private void UseItemOnStudent(StudentAgent student, GameObject item)
        {
            if (student == null || item == null) return;

            Debug.Log($"[TeacherController] Using {item.name} on {student.Config?.studentName}");

            HeldItem heldItem = item.GetComponent<HeldItem>();
            if (heldItem != null && heldItem.itemName.ToLower().Contains("whistle"))
            {
                student.StopCurrentAction();
                student.CalmDown();
            }
            else
            {
                student.TakeObjectAway(item);
            }
        }

        /// <summary>
        /// Determines if a student is outside the classroom based on LevelConfig locations
        /// </summary>
        private bool IsStudentOutsideClassroom(StudentAgent student)
        {
            if (student == null || LevelManager.Instance == null) return false;

            LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
            if (currentLevel == null)
            {
                Debug.LogWarning("[Teacher] LevelConfig is null - cannot detect outside");
                return false;
            }
            
            if (currentLevel.classroomDoor == null)
            {
                Debug.LogWarning("[Teacher] classroomDoor is null - cannot detect outside");
                return false;
            }

            // Check if student is beyond the classroom door
            Vector3 doorPosition = currentLevel.classroomDoor.position;
            Vector3 studentPosition = student.transform.position;
            
            // Simple distance check - can be made more sophisticated
            float distanceFromDoor = Vector3.Distance(studentPosition, doorPosition);
            
            // If student is far from their seat and near/past the door, they're outside
            float distanceFromSeat = Vector3.Distance(studentPosition, student.OriginalSeatPosition);
            
            bool isOutside = distanceFromSeat > 5f && distanceFromDoor < 10f;
            
            Debug.Log($"[Teacher] {student.Config?.studentName} - distFromSeat: {distanceFromSeat:F2}m, distFromDoor: {distanceFromDoor:F2}m, isOutside: {isOutside}");
            
            return isOutside;
        }

        /// <summary>
        /// Gets a context-sensitive interaction prompt based on student location and state
        /// </summary>
        private string GetContextualPrompt(StudentAgent student)
        {
            if (student == null || student.Config == null) return "Interact with student";

            string studentName = student.Config.studentName;
            
            // Check if student is outside classroom
            bool isOutside = IsStudentOutsideClassroom(student);
            
            // Check if student is following a route
            bool isOnRoute = student.IsFollowingRoute;
            StudentRoute currentRoute = student.GetCurrentRoute();

            if (isOutside || isOnRoute)
            {
                // Student is outside or escaping
                if (student.CurrentState == StudentState.Critical)
                {
                    return $"Press E to escort {studentName} back to class";
                }
                else
                {
                    return $"Press E to call {studentName} back to class";
                }
            }
            else
            {
                // Normal classroom interaction
                return student.CurrentState switch
                {
                    StudentState.Calm => $"Talk to {studentName}",
                    StudentState.Distracted => $"Calm down {studentName}",
                    StudentState.ActingOut => $"Stop {studentName}",
                    StudentState.Critical => $"Send {studentName} back to seat",
                    _ => $"Interact with {studentName}"
                };
            }
        }

        /// <summary>
        /// Handles context-sensitive interaction based on student location and state
        /// </summary>
        private void HandleContextualInteraction(StudentAgent student)
        {
            if (student == null) return;

            bool isOutside = IsStudentOutsideClassroom(student);
            bool isOnRoute = student.IsFollowingRoute;
            
            Debug.Log($"[Teacher] Interacting with {student.Config?.studentName} - isOutside: {isOutside}, isOnRoute: {isOnRoute}, state: {student.CurrentState}");

            if (isOutside || isOnRoute)
            {
                // Student is outside or escaping - use recall actions
                Debug.Log($"[Teacher] Student is outside/on route - triggering recall");
                if (student.CurrentState == StudentState.Critical)
                {
                    EscortStudentBack(student);
                }
                else
                {
                    CallStudentBack(student);
                }
            }
            else
            {
                // Normal classroom interaction
                Debug.Log($"[Teacher] Student not outside - normal interaction");
                InteractWithStudent(student);
            }
        }

        /// <summary>
        /// Calls a student back to class using the return route
        /// </summary>
        private void CallStudentBack(StudentAgent student)
        {
            if (student == null) return;

            Debug.Log($"[Teacher] Called {student.Config?.studentName} back to class");

            // Calm down student completely when teacher escorts
            StudentState originalState = student.CurrentState;
            if (originalState != StudentState.Calm)
            {
                Debug.Log($"[Teacher] Calming down {student.Config?.studentName} from {originalState}...");
                
                int deescalateCount = 0;
                while (student.CurrentState != StudentState.Calm && deescalateCount < 10)
                {
                    student.DeescalateState();
                    deescalateCount++;
                }
                
                Debug.Log($"[Teacher] ✓ Calmed down {student.Config?.studentName} ({originalState} → {student.CurrentState})");
            }
            
            // Set influence immunity to prevent re-escalation
            student.SetInfluenceImmunity(15f); // 15 seconds immunity after escort
            
            // Stop current route/escape behavior
            student.StopRoute();

            // Unregister from outside tracking (they're being recalled)
            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.UnregisterStudentOutside(student);
            }

            // Get return route from level config
            if (LevelManager.Instance != null)
            {
                LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
                Debug.Log($"[Teacher] LevelConfig: {(currentLevel != null ? "exists" : "null")}, ReturnRoute: {(currentLevel?.returnRoute != null ? currentLevel.returnRoute.routeName : "null")}");
                
                if (currentLevel != null && currentLevel.returnRoute != null)
                {
                    Debug.Log($"[Teacher] Starting return route: {currentLevel.returnRoute.routeName}");
                    student.StartRoute(currentLevel.returnRoute);
                    Debug.Log($"[Teacher] {student.Config?.studentName} started return route");
                }
                else
                {
                    // Fallback: direct return to seat with visual movement
                    Debug.Log($"[Teacher] No return route available, using direct return to seat");
                    if (StudentMovementManager.Instance != null)
                    {
                        StudentMovementManager.Instance.ReturnToSeat(student);
                    }
                    else
                    {
                        student.ReturnToSeat(); // Teleport fallback
                    }
                }
            }
            else
            {
                Debug.Log($"[Teacher] LevelManager.Instance is null, using direct return to seat");
                if (StudentMovementManager.Instance != null)
                {
                    StudentMovementManager.Instance.ReturnToSeat(student);
                }
                else
                {
                    student.ReturnToSeat(); // Teleport fallback
                }
            }

            // Trigger teacher action
            student.HandleTeacherAction(TeacherActionType.CallStudentBack);

            // Reduce classroom disruption
            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-10f, $"{student.Config?.studentName} called back");
            }

            // Log event
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.StudentCalmed,
                    "was called back to class by teacher"
                );
            }
        }

        /// <summary>
        /// Checks for students outside classroom and automatically escorts them back
        /// DISABLED - Player must manually escort students
        /// </summary>
        private void CheckAndEscortOutsideStudents()
        {
            // Auto-escort disabled - player must manually interact with students to escort them back
            return;
            
            // Throttle checks to avoid performance issues
            if (Time.time - lastEscortCheckTime < escortCheckInterval)
                return;
            
            lastEscortCheckTime = Time.time;
            
            // Get all students who are currently outside
            if (ClassroomManager.Instance == null) return;
            
            StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();
            foreach (StudentAgent student in allStudents)
            {
                if (student == null) continue;
                
                // Check if student is outside classroom
                bool isOutside = IsStudentOutsideClassroom(student);
                if (!isOutside) continue;
                
                // Check how long they've been outside
                float timeOutside = ClassroomManager.Instance.GetStudentOutsideDuration(student);
                
                // Auto-escort if they've been outside for more than 3 seconds
                if (timeOutside > 3f)
                {
                    Debug.Log($"[Teacher] Auto-escorting {student.Config?.studentName} (outside for {timeOutside:F1}s)");
                    
                    if (student.CurrentState == StudentState.Critical)
                    {
                        EscortStudentBack(student);
                    }
                    else
                    {
                        CallStudentBack(student);
                    }
                }
            }
        }

        /// <summary>
        /// Escorts a critical student back to their seat
        /// </summary>
        private void EscortStudentBack(StudentAgent student)
        {
            if (student == null) return;

            Debug.Log($"[Teacher] Attempting to escort {student.Config?.studentName} back to seat");

            // Check if all influence sources are resolved
            if (student.InfluenceSources != null && !student.InfluenceSources.AreAllSourcesResolved())
            {
                int unresolvedCount = student.InfluenceSources.GetUnresolvedSourceCount();
                var unresolvedSources = student.InfluenceSources.GetUnresolvedSourceStudents();
                
                Debug.LogWarning($"[Teacher] ✗ Cannot escort {student.Config?.studentName} - {unresolvedCount} unresolved influence sources!");
                
                foreach (var source in unresolvedSources)
                {
                    Debug.LogWarning($"[Teacher]   - Unresolved source: {source.Config?.studentName}");
                }

                // Student returns to outdoor (escape route)
                Debug.Log($"[Teacher] {student.Config?.studentName} returning to outdoor due to unresolved sources");
                
                if (LevelManager.Instance != null)
                {
                    LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
                    if (currentLevel != null && currentLevel.escapeRoute != null)
                    {
                        student.StartRoute(currentLevel.escapeRoute);
                    }
                }

                // Add disruption for failed escort
                if (ClassroomManager.Instance != null)
                {
                    float disruptionPenalty = 10f * unresolvedCount; // 10 points per unresolved source
                    ClassroomManager.Instance.AddDisruption(disruptionPenalty, 
                        $"{student.Config?.studentName} returned to outdoor - {unresolvedCount} unresolved sources");
                    
                    Debug.LogWarning($"[Teacher] Added {disruptionPenalty} disruption for failed escort");
                }

                return;
            }

            Debug.Log($"[Teacher] ✓ All influence sources resolved - proceeding with escort");

            // Calm down student completely when teacher escorts
            StudentState originalState = student.CurrentState;
            if (originalState != StudentState.Calm)
            {
                Debug.Log($"[Teacher] Calming down {student.Config?.studentName} from {originalState}...");
                
                int deescalateCount = 0;
                while (student.CurrentState != StudentState.Calm && deescalateCount < 10)
                {
                    student.DeescalateState();
                    deescalateCount++;
                }
                
                Debug.Log($"[Teacher] ✓ Calmed down {student.Config?.studentName} ({originalState} → {student.CurrentState})");
            }
            
            // Clear all influence sources after successful escort
            if (student.InfluenceSources != null)
            {
                student.InfluenceSources.ClearAllSources();
            }
            
            // Set influence immunity to prevent re-escalation
            student.SetInfluenceImmunity(15f); // 15 seconds immunity after escort
            
            // Stop current behavior
            student.StopRoute();

            // Unregister from outside tracking
            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.UnregisterStudentOutside(student);
            }

            // Force return with visual movement
            Debug.Log($"[Teacher] Returning {student.Config?.studentName} to seat with visual movement");
            if (StudentMovementManager.Instance != null)
            {
                StudentMovementManager.Instance.ReturnToSeat(student);
            }
            else
            {
                student.ReturnToSeat(); // Teleport fallback
            }

            // Trigger teacher action
            student.HandleTeacherAction(TeacherActionType.EscortStudentBack);

            // Significant disruption reduction
            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-15f, $"{student.Config?.studentName} escorted back");
            }

            // Log event
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.StudentReturnedToSeat,
                    "was escorted back to seat by teacher"
                );
            }
        }

        /// <summary>
        /// Forces a student to return to their seat (can be called via input)
        /// </summary>
        public void ForceStudentReturnToSeat(StudentAgent student)
        {
            if (student == null) return;

            Debug.Log($"[Teacher] Forcing {student.Config?.studentName} to return to seat");

            student.StopRoute();
            student.ReturnToSeat();
            student.HandleTeacherAction(TeacherActionType.ForceReturnToSeat);

            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-5f, $"{student.Config?.studentName} forced to seat");
            }
        }

        /// <summary>
        /// Cleans a mess object
        /// </summary>
        private void CleanMess(MessObject mess)
        {
            if (mess == null) return;

            if (mess.IsCleaned())
            {
                Debug.LogWarning($"[Teacher] Attempted to clean already cleaned {mess.messName}");
                return;
            }

            Debug.Log($"[Teacher] Cleaning {mess.messName}");

            // Start cleanup process
            mess.StartCleanup(this);

            // Clear current target
            currentMessTarget = null;
        }

        /// <summary>
        /// Gets the current mess target the teacher is looking at
        /// </summary>
        public MessObject GetCurrentMessTarget()
        {
            return currentMessTarget;
        }
    }
}
