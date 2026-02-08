using UnityEngine;
using FunClass.Core.UI;

namespace FunClass.Core
{
    [RequireComponent(typeof(CharacterController))]
    public class TeacherController : MonoBehaviour
    {
        public static TeacherController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float movementDeadZone = 0.15f;

        [Header("Camera")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 80f;
        [SerializeField] private float mouseDeadZone = 0.05f;

        [Header("Item Holding")]
        [SerializeField] private Transform itemHoldPoint;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionLayer = ~0;

        [Header("Auto Escort")]
        [SerializeField] private bool autoEscortEnabled = false;
        [SerializeField] private float escortCheckInterval = 2f;
        private float lastEscortCheckTime;

        private CharacterController characterController;
        private HeldItem currentHeldItem;
        private float verticalVelocity;
        private float cameraPitch;
        private bool isActive = false;
        private bool hasSubscribedToGameState = false;
        private InteractableObject currentLookTarget;
        private StudentAgent currentStudentTarget;
        private MessObject currentMessTarget;
        private UI.StudentHighlight currentHighlight;

        public Camera PlayerCamera => playerCamera;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                GameLogger.Warning("TeacherController", "Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            characterController = GetComponent<CharacterController>();
            
            if (characterController == null)
            {
                GameLogger.Error("TeacherController", "CharacterController component missing!");
            }
            else
            {
                GameLogger.Detail("TeacherController", "CharacterController found");
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                GameLogger.Detail("TeacherController", $"Camera assigned from Camera.main: {playerCamera != null}");
            }

            if (cameraTransform == null && playerCamera != null)
            {
                cameraTransform = playerCamera.transform;
            }
            
            GameLogger.Detail("TeacherController", $"Awake complete - Camera: {playerCamera != null}");
        }

        void OnEnable()
        {
            GameLogger.Detail("TeacherController", "OnEnable called");
            
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                hasSubscribedToGameState = true;
                GameLogger.Detail("TeacherController", $"Subscribed to OnStateChanged - state: {GameStateManager.Instance.CurrentState}");
            }
            else
            {
                GameLogger.Warning("TeacherController", "GameStateManager.Instance is NULL in OnEnable");
            }
        }

        void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
                hasSubscribedToGameState = false;
            }
        }

        void Start()
        {
            GameLogger.Detail("TeacherController", "Start called");
            
            // Fallback subscription
            if (GameStateManager.Instance != null)
            {
                GameLogger.Detail("TeacherController", $"Current game state: {GameStateManager.Instance.CurrentState}");
                
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                hasSubscribedToGameState = true;
                GameLogger.Detail("TeacherController", "Subscribed to OnStateChanged in Start()");
                
                if (GameStateManager.Instance.CurrentState == GameState.InLevel)
                {
                    GameLogger.Detail("TeacherController", "Already in InLevel, activating teacher");
                    ActivateTeacher();
                }
            }
            else
            {
                GameLogger.Error("TeacherController", "GameStateManager.Instance is STILL null in Start()!");
            }

            // Hide cursor
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.HideCursor();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void Update()
        {
            if (!isActive) return;

            GameLogger.Trace("TeacherController", $"Update - isActive={isActive}");

            HandleMovement();
            
            if (!PopupManager.Instance.IsPopupOpen)
            {
                HandleCamera();
            }
            
            HandleInteractionDetection();
            HandleInteractionInput();
            
            if (autoEscortEnabled)
            {
                CheckAndEscortOutsideStudents();
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            GameLogger.Detail("TeacherController", $"{oldState} → {newState}");
            
            if (newState == GameState.InLevel)
            {
                ActivateTeacher();
            }
            else
            {
                DeactivateTeacher();
            }
        }

        private void ActivateTeacher()
        {
            isActive = true;

            // Hide cursor
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.HideCursor();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            GameLogger.Milestone("TeacherController", "Activated - teacher control enabled");
        }

        private void DeactivateTeacher()
        {
            isActive = false;
            GameLogger.Detail("TeacherController", "Deactivated");
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            GameLogger.Trace("TeacherController", 
                $"Input raw - horizontal: {horizontal:F3}, vertical: {vertical:F3}");
            
            // Apply dead zone
            if (Mathf.Abs(horizontal) < movementDeadZone) horizontal = 0f;
            if (Mathf.Abs(vertical) < movementDeadZone) vertical = 0f;

            if (horizontal != 0f || vertical != 0f)
            {
                GameLogger.Detail("TeacherController", 
                    $"Input after dead zone - horizontal: {horizontal:F3}, vertical: {vertical:F3}, grounded: {characterController.isGrounded}");
            }

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

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            GameLogger.Trace("TeacherController", 
                $"Mouse raw - X: {mouseX:F3}, Y: {mouseY:F3}");
            
            // Apply dead zone
            if (Mathf.Abs(mouseX) < mouseDeadZone) mouseX = 0f;
            if (Mathf.Abs(mouseY) < mouseDeadZone) mouseY = 0f;

            // Apply sensitivity
            mouseX *= lookSensitivity;
            mouseY *= lookSensitivity;

            if (mouseX != 0f || mouseY != 0f)
            {
                GameLogger.Detail("TeacherController", 
                    $"Mouse after dead zone - X: {mouseX:F3}, Y: {mouseY:F3}");
            }

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

            GameLogger.Detail("TeacherController", $"Holding: {item?.itemName ?? "nothing"}");
        }

        public void DropItem()
        {
            if (currentHeldItem == null) return;

            GameLogger.Detail("TeacherController", $"Dropped: {currentHeldItem.itemName}");

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
                        if (currentHighlight != null)
                        {
                            currentHighlight.SetHighlight(false);
                        }
                        
                        currentStudentTarget = student;
                        currentLookTarget = null;
                        
                        currentHighlight = student.GetComponent<UI.StudentHighlight>();
                        if (currentHighlight == null)
                        {
                            currentHighlight = student.gameObject.AddComponent<UI.StudentHighlight>();
                        }
                        currentHighlight.SetHighlight(true);

                        string prompt = GetContextualPrompt(student);
                        GameLogger.Detail("TeacherController", $"Looking at: {prompt}");
                    }

                    // Show cursor
                    if (CursorManager.Instance != null)
                    {
                        CursorManager.Instance.ShowCursor();
                    }
                    else
                    {
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.None;
                    }
                }
                else
                {
                    // Check for mess objects
                    MessObject mess = hit.collider.GetComponent<MessObject>();
                    if (mess != null)
                    {
                        if (mess != currentMessTarget)
                        {
                            currentMessTarget = mess;
                            currentLookTarget = null;
                            currentStudentTarget = null;
                            
                            GameLogger.Detail("TeacherController", $"Looking at: {currentMessTarget.GetInteractionPrompt()}");
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
                                GameLogger.Detail("TeacherController", $"Looking at: {currentLookTarget.GetInteractionPrompt()}");
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
                    if (currentHighlight != null)
                    {
                        currentHighlight.SetHighlight(false);
                        currentHighlight = null;
                    }
                    currentStudentTarget = null;
                }
                if (currentMessTarget != null)
                {
                    currentMessTarget = null;
                }

                if (!PopupManager.Instance.IsPopupOpen)
                {
                    if (CursorManager.Instance != null)
                    {
                        CursorManager.Instance.HideCursor();
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
        }

        private void HandleInteractionInput()
        {
            // Left Click - Show popup for student
            if (Input.GetMouseButtonDown(0))
            {
                if (currentStudentTarget != null && !PopupManager.Instance.IsPopupOpen)
                {
                    PopupManager.Instance.ShowPopup(currentStudentTarget);
                }
            }

            // E key - Calm student
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (currentMessTarget != null)
                {
                    CleanMess(currentMessTarget);
                }
                else if (currentStudentTarget != null)
                {
                    CalmStudent(currentStudentTarget);
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

            GameLogger.Detail("TeacherController", $"Using {item.name} on {student.Config?.studentName}");

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

        private bool IsStudentOutsideClassroom(StudentAgent student)
        {
            if (student == null || LevelManager.Instance == null) return false;

            LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
            if (currentLevel == null || currentLevel.classroomDoor == null)
            {
                return false;
            }

            Vector3 doorPosition = currentLevel.classroomDoor.position;
            Vector3 studentPosition = student.transform.position;
            
            float distanceFromDoor = Vector3.Distance(studentPosition, doorPosition);
            float distanceFromSeat = Vector3.Distance(studentPosition, student.OriginalSeatPosition);
            
            bool isOutside = distanceFromSeat > 5f && distanceFromDoor < 10f;
            
            GameLogger.Detail("TeacherController", 
                $"{student.Config?.studentName}: distFromSeat={distanceFromSeat:F1}m, distFromDoor={distanceFromDoor:F1}m, isOutside={isOutside}");
            
            return isOutside;
        }

        private string GetContextualPrompt(StudentAgent student)
        {
            if (student == null || student.Config == null) return "Interact with student";

            string studentName = student.Config.studentName;
            bool isOutside = IsStudentOutsideClassroom(student);
            bool isOnRoute = student.IsFollowingRoute;

            if (isOutside || isOnRoute)
            {
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

        private void HandleContextualInteraction(StudentAgent student)
        {
            if (student == null) return;

            bool isOutside = IsStudentOutsideClassroom(student);
            bool isOnRoute = student.IsFollowingRoute;
            
            GameLogger.Detail("TeacherController", 
                $"Interacting with {student.Config?.studentName}: isOutside={isOutside}, isOnRoute={isOnRoute}, state={student.CurrentState}");

            if (isOutside || isOnRoute)
            {
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
                InteractWithStudent(student);
            }
        }

        private void CalmStudent(StudentAgent student)
        {
            if (student == null) return;

            GameLogger.Milestone("TeacherController", $"Calming {student.Config?.studentName}");

            StudentState originalState = student.CurrentState;
            if (originalState != StudentState.Calm)
            {
                int deescalateCount = 0;
                while (student.CurrentState != StudentState.Calm && deescalateCount < 10)
                {
                    student.DeescalateState();
                    deescalateCount++;
                }
                GameLogger.Detail("TeacherController", 
                    $"{student.Config?.studentName} de-escalated: {originalState} → {student.CurrentState}");
            }

            student.HandleTeacherAction(TeacherActionType.Calm);
            
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.StudentCalmed,
                    $"Teacher calmed {student.Config?.studentName}",
                    null
                );
            }

            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-5f, $"Calmed {student.Config?.studentName}");
            }
        }

        private void CallStudentBack(StudentAgent student)
        {
            if (student == null) return;

            GameLogger.Milestone("TeacherController", $"Calling {student.Config?.studentName} back to class");

            StudentState originalState = student.CurrentState;
            if (originalState != StudentState.Calm)
            {
                int deescalateCount = 0;
                while (student.CurrentState != StudentState.Calm && deescalateCount < 10)
                {
                    student.DeescalateState();
                    deescalateCount++;
                }
                GameLogger.Detail("TeacherController", 
                    $"{student.Config?.studentName} calmed: {originalState} → {student.CurrentState}");
            }

            student.SetInfluenceImmunity(15f);
            student.StopRoute();

            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.UnregisterStudentOutside(student);
            }

            if (LevelManager.Instance != null)
            {
                LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
                
                if (currentLevel != null && currentLevel.returnRoute != null)
                {
                    student.StartRoute(currentLevel.returnRoute);
                    GameLogger.Detail("TeacherController", 
                        $"{student.Config?.studentName} started return route: {currentLevel.returnRoute.routeName}");
                }
                else
                {
                    if (StudentMovementManager.Instance != null)
                    {
                        StudentMovementManager.Instance.ReturnToSeat(student);
                    }
                    else
                    {
                        student.ReturnToSeat();
                    }
                }
            }

            student.HandleTeacherAction(TeacherActionType.CallStudentBack);

            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-10f, $"{student.Config?.studentName} called back");
            }

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.StudentCalmed,
                    "was called back to class by teacher"
                );
            }
        }

        private void CheckAndEscortOutsideStudents()
        {
            return;
        }

        private void EscortStudentBack(StudentAgent student)
        {
            if (student == null) return;

            GameLogger.Milestone("TeacherController", $"Escorting {student.Config?.studentName} back to seat");

            // Check if all influence sources are resolved
            if (student.InfluenceSources != null && !student.InfluenceSources.AreAllSourcesResolved())
            {
                int unresolvedCount = student.InfluenceSources.GetUnresolvedSourceCount();
                var unresolvedSources = student.InfluenceSources.GetUnresolvedSourceStudents();
                
                GameLogger.Warning("TeacherController", 
                    $"Cannot escort {student.Config?.studentName} - {unresolvedCount} unresolved influence sources!");
                
                foreach (var source in unresolvedSources)
                {
                    GameLogger.Warning("TeacherController", $"  - Unresolved: {source.Config?.studentName}");
                }

                if (LevelManager.Instance != null)
                {
                    LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
                    if (currentLevel != null && currentLevel.escapeRoute != null)
                    {
                        student.StartRoute(currentLevel.escapeRoute);
                    }
                }

                if (ClassroomManager.Instance != null)
                {
                    float penaltyPerSource = 10f;
                    ClassroomManager.Instance.AddDisruption(penaltyPerSource * unresolvedCount, 
                        $"{student.Config?.studentName} returned to outdoor");
                }
                return;
            }

            StudentState originalState = student.CurrentState;
            if (originalState != StudentState.Calm)
            {
                int deescalateCount = 0;
                while (student.CurrentState != StudentState.Calm && deescalateCount < 10)
                {
                    student.DeescalateState();
                    deescalateCount++;
                }
                GameLogger.Detail("TeacherController", 
                    $"{student.Config?.studentName} calmed: {originalState} → {student.CurrentState}");
            }

            if (student.InfluenceSources != null)
            {
                student.InfluenceSources.ClearAllSources();
            }

            student.SetInfluenceImmunity(15f);
            student.StopRoute();

            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.UnregisterStudentOutside(student);
            }

            if (StudentMovementManager.Instance != null)
            {
                StudentMovementManager.Instance.ReturnToSeat(student);
            }
            else
            {
                student.ReturnToSeat();
            }

            student.HandleTeacherAction(TeacherActionType.EscortStudentBack);

            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-15f, $"{student.Config?.studentName} escorted back");
            }

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.StudentReturnedToSeat,
                    "was escorted back to seat by teacher"
                );
            }
        }

        public void ForceStudentReturnToSeat(StudentAgent student)
        {
            if (student == null) return;

            GameLogger.Detail("TeacherController", $"Forcing {student.Config?.studentName} to return to seat");

            student.StopRoute();
            student.ReturnToSeat();
            student.HandleTeacherAction(TeacherActionType.ForceReturnToSeat);

            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-5f, $"{student.Config?.studentName} forced to seat");
            }
        }

        private void CleanMess(MessObject mess)
        {
            if (mess == null) return;

            if (mess.IsCleaned())
            {
                GameLogger.Warning("TeacherController", $"Attempted to clean already cleaned {mess.messName}");
                return;
            }

            GameLogger.Detail("TeacherController", $"Cleaning {mess.messName}");

            mess.StartCleanup(this);
            currentMessTarget = null;
        }

        public MessObject GetCurrentMessTarget()
        {
            return currentMessTarget;
        }
    }
}
