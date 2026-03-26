// === BRAIN BUSTER - JavaScript ===
// Kein Framework, einfach vanilla JS - so wie es sein soll

// API Base URL
const API_URL = '';

// Globale Variablen für das Spiel
let currentSession = null;
let authToken = localStorage.getItem('authToken');
let currentUser = null;
let timerInterval = null;
let timeLeft = 30;

// === INIT ===
// Beim Laden der Seite ausführen
document.addEventListener('DOMContentLoaded', () => {
    loadCategories();
    checkAuth();
    loadLeaderboard();
});

// === VIEW MANAGEMENT ===
// Zwischen verschiedenen Views wechseln
function showView(viewName) {
    // Alle Views verstecken
    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    // Gewünschte View anzeigen
    document.getElementById(viewName + 'View').classList.add('active');
    
    // Timer stoppen wenn wir das Spiel verlassen
    if (viewName !== 'game') {
        stopTimer();
    }
    
    // Leaderboard laden wenn wir dorthin wechseln
    if (viewName === 'leaderboard') {
        loadLeaderboard();
    }
    
    // Admin: Fragen laden
    if (viewName === 'admin') {
        loadAdminQuestions();
    }
}

// === AUTH ===
// Login Tab wechseln
function showAuthTab(tab) {
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.auth-tab').forEach(t => t.classList.remove('active'));
    
    document.querySelector(`[onclick="showAuthTab('${tab}')"]`).classList.add('active');
    document.getElementById(tab + 'Tab').classList.add('active');
    document.getElementById('authError').textContent = '';
}

// Registrieren
async function register() {
    const username = document.getElementById('registerUsername').value.trim();
    const password = document.getElementById('registerPassword').value;
    
    if (username.length < 3) {
        document.getElementById('authError').textContent = 'Username muss min. 3 Zeichen haben!';
        return;
    }
    if (password.length < 4) {
        document.getElementById('authError').textContent = 'Passwort muss min. 4 Zeichen haben!';
        return;
    }
    
    try {
        const res = await fetch(API_URL + '/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });
        
        const data = await res.json();
        
        if (data.error) {
            document.getElementById('authError').textContent = data.error;
            return;
        }
        
        // Erfolgreich - Token speichern
        authToken = data.token;
        localStorage.setItem('authToken', authToken);
        currentUser = data.player;
        updateAuthUI();
        
    } catch (err) {
        document.getElementById('authError').textContent = 'Verbindungsfehler!';
    }
}

// Login
async function login() {
    const username = document.getElementById('loginUsername').value.trim();
    const password = document.getElementById('loginPassword').value;
    
    try {
        const res = await fetch(API_URL + '/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });
        
        const data = await res.json();
        
        if (data.error) {
            document.getElementById('authError').textContent = data.error;
            return;
        }
        
        authToken = data.token;
        localStorage.setItem('authToken', authToken);
        currentUser = data.player;
        updateAuthUI();
        
    } catch (err) {
        document.getElementById('authError').textContent = 'Verbindungsfehler!';
    }
}

// Logout
function logout() {
    authToken = null;
    currentUser = null;
    localStorage.removeItem('authToken');
    updateAuthUI();
}

// Auth Status prüfen
async function checkAuth() {
    if (!authToken) {
        updateAuthUI();
        return;
    }
    
    try {
        const res = await fetch(API_URL + '/api/auth/me', {
            headers: { 'Authorization': 'Bearer ' + authToken }
        });
        
        const data = await res.json();
        
        if (data.loggedIn) {
            currentUser = data.player;
        } else {
            authToken = null;
            localStorage.removeItem('authToken');
        }
        
    } catch (err) {
        console.log('Auth check fehlgeschlagen');
    }
    
    updateAuthUI();
}

// UI entsprechend Auth Status updaten
function updateAuthUI() {
    const authSection = document.getElementById('authSection');
    const playerInfo = document.getElementById('playerInfoSection');
    const guestNameInput = document.getElementById('guestName');
    const userStatus = document.getElementById('userStatus');
    
    if (currentUser) {
        // Eingeloggt
        authSection.classList.add('hidden');
        playerInfo.classList.remove('hidden');
        guestNameInput.parentElement.classList.add('hidden');
        
        document.getElementById('playerName').textContent = currentUser.username;
        document.getElementById('playerScore').textContent = currentUser.totalScore;
        document.getElementById('playerGames').textContent = currentUser.gamesPlayed;
        document.getElementById('playerAccuracy').textContent = currentUser.accuracy + '%';
        
        userStatus.textContent = '👤 ' + currentUser.username;
    } else {
        // Nicht eingeloggt
        authSection.classList.remove('hidden');
        playerInfo.classList.add('hidden');
        guestNameInput.parentElement.classList.remove('hidden');
        userStatus.textContent = '';
    }
}

// === KATEGORIEN ===
async function loadCategories() {
    try {
        const res = await fetch(API_URL + '/api/categories');
        const categories = await res.json();
        
        const select = document.getElementById('categorySelect');
        const adminSelect = document.getElementById('newQuestionCategory');
        
        // Selects leeren (außer erste Option)
        select.innerHTML = '<option value="0">Alle Kategorien</option>';
        adminSelect.innerHTML = '<option value="0">Keine Kategorie</option>';
        
        categories.forEach(cat => {
            select.innerHTML += `<option value="${cat.id}">${cat.name}</option>`;
            adminSelect.innerHTML += `<option value="${cat.id}">${cat.name}</option>`;
        });
        
    } catch (err) {
        console.log('Kategorien laden fehlgeschlagen:', err);
    }
}

// === SPIEL ===
async function startGame() {
    const categoryId = parseInt(document.getElementById('categorySelect').value);
    const difficulty = document.getElementById('difficultySelect').value;
    const questionCount = parseInt(document.getElementById('questionCountSelect').value);
    const playerName = currentUser ? currentUser.username : 
        (document.getElementById('guestName').value.trim() || 'Gast');
    
    try {
        const headers = { 'Content-Type': 'application/json' };
        if (authToken) {
            headers['Authorization'] = 'Bearer ' + authToken;
        }
        
        const res = await fetch(API_URL + '/api/game/start', {
            method: 'POST',
            headers,
            body: JSON.stringify({ categoryId, difficulty, questionCount, playerName })
        });
        
        const data = await res.json();
        
        if (data.error) {
            alert('Fehler: ' + data.error);
            return;
        }
        
        currentSession = {
            sessionId: data.sessionId,
            totalQuestions: data.totalQuestions,
            score: data.score,
            streak: data.streak
        };
        
        showView('game');
        displayQuestion(data.currentQuestion);
        startTimer();
        
    } catch (err) {
        alert('Verbindungsfehler! Ist der Server gestartet?');
        console.error(err);
    }
}

// Frage anzeigen
function displayQuestion(question) {
    if (!question) {
        showResult();
        return;
    }
    
    document.getElementById('questionNumber').textContent = 
        question.number + '/' + question.total;
    document.getElementById('gameScore').textContent = currentSession.score;
    document.getElementById('gameStreak').textContent = currentSession.streak + '🔥';
    document.getElementById('questionText').textContent = question.text;
    
    // Difficulty Badge
    const badge = document.getElementById('difficultyBadge');
    badge.textContent = question.difficulty.charAt(0).toUpperCase() + question.difficulty.slice(1);
    badge.className = 'difficulty-badge ' + question.difficulty;
    
    // Antworten anzeigen
    const container = document.getElementById('answersContainer');
    container.innerHTML = '';
    
    question.answers.forEach((answer, index) => {
        const btn = document.createElement('button');
        btn.className = 'answer-btn';
        btn.textContent = answer;
        btn.onclick = () => selectAnswer(answer, btn);
        container.appendChild(btn);
    });
    
    // Feedback verstecken
    document.getElementById('feedbackContainer').classList.add('hidden');
    document.getElementById('answersContainer').classList.remove('hidden');
    
    // Timer resetten
    resetTimer();
}

// Antwort auswählen
async function selectAnswer(answer, btn) {
    // Alle Buttons deaktivieren
    document.querySelectorAll('.answer-btn').forEach(b => {
        b.disabled = true;
    });
    
    stopTimer();
    
    // Auswahl markieren
    btn.classList.add('selected');
    
    try {
        const res = await fetch(API_URL + '/api/game/answer', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                sessionId: currentSession.sessionId, 
                answer 
            })
        });
        
        const data = await res.json();
        
        // Session updaten
        currentSession.score = data.totalScore;
        currentSession.streak = data.streak;
        currentSession.correctAnswers = data.correctAnswers;
        currentSession.isFinished = data.isFinished;
        currentSession.nextQuestion = data.currentQuestion;
        
        // Richtige/falsche Antwort anzeigen
        document.querySelectorAll('.answer-btn').forEach(b => {
            if (b.textContent === data.correctAnswer) {
                b.classList.add('correct');
            } else if (b.classList.contains('selected') && !data.isCorrect) {
                b.classList.add('wrong');
            }
        });
        
        // Feedback anzeigen
        showFeedback(data.isCorrect, data.pointsEarned, data.correctAnswer);
        
    } catch (err) {
        console.error('Fehler beim Antworten:', err);
    }
}

// Feedback anzeigen
function showFeedback(isCorrect, points, correctAnswer) {
    const container = document.getElementById('feedbackContainer');
    const icon = document.getElementById('feedbackIcon');
    const text = document.getElementById('feedbackText');
    const pointsEl = document.getElementById('feedbackPoints');
    
    if (isCorrect) {
        icon.textContent = '✅';
        text.textContent = 'Richtig!';
        pointsEl.textContent = '+' + points + ' Punkte';
    } else {
        icon.textContent = '❌';
        text.textContent = 'Falsch! Richtig war: ' + correctAnswer;
        pointsEl.textContent = '+0 Punkte';
    }
    
    container.classList.remove('hidden');
    
    // Punktestand updaten
    document.getElementById('gameScore').textContent = currentSession.score;
    document.getElementById('gameStreak').textContent = currentSession.streak + '🔥';
}

// Nächste Frage oder Ergebnis
function nextQuestion() {
    if (currentSession.isFinished) {
        showResult();
    } else {
        displayQuestion(currentSession.nextQuestion);
        startTimer();
    }
}

// Ergebnis anzeigen
function showResult() {
    stopTimer();
    showView('result');
    
    const totalQuestions = currentSession.totalQuestions;
    const correct = currentSession.correctAnswers || 0;
    const accuracy = totalQuestions > 0 ? Math.round(correct / totalQuestions * 100) : 0;
    
    document.getElementById('resultScore').textContent = currentSession.score;
    document.getElementById('resultCorrect').textContent = correct + '/' + totalQuestions;
    document.getElementById('resultAccuracy').textContent = accuracy + '%';
    
    // Bewertung
    let rating = '';
    if (accuracy >= 90) rating = '🏆 PERFEKT! Du bist ein Genie!';
    else if (accuracy >= 70) rating = '⭐ Sehr gut! Weiter so!';
    else if (accuracy >= 50) rating = '👍 Nicht schlecht!';
    else rating = '📚 Mehr üben! Du schaffst das!';
    
    document.getElementById('resultRating').textContent = rating;
    
    // Leaderboard aktualisieren
    loadLeaderboard();
    
    // User Stats aktualisieren falls eingeloggt
    if (currentUser) {
        checkAuth();
    }
}

// === TIMER ===
function startTimer() {
    timeLeft = 30;
    updateTimerDisplay();
    
    timerInterval = setInterval(() => {
        timeLeft--;
        updateTimerDisplay();
        
        if (timeLeft <= 0) {
            // Zeit abgelaufen - automatisch falsche Antwort
            selectAnswer('', null);
        }
    }, 1000);
}

function stopTimer() {
    if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
    }
}

function resetTimer() {
    timeLeft = 30;
    updateTimerDisplay();
}

function updateTimerDisplay() {
    const timerEl = document.getElementById('timer');
    timerEl.textContent = timeLeft;
    
    // Farbe ändern wenn wenig Zeit
    if (timeLeft <= 5) {
        timerEl.style.color = 'var(--error)';
    } else if (timeLeft <= 10) {
        timerEl.style.color = 'var(--warning)';
    } else {
        timerEl.style.color = '';
    }
}

// === LEADERBOARD ===
async function loadLeaderboard() {
    try {
        const res = await fetch(API_URL + '/api/leaderboard');
        const players = await res.json();
        
        const body = document.getElementById('leaderboardBody');
        body.innerHTML = '';
        
        if (players.length === 0) {
            body.innerHTML = '<div class="leaderboard-row"><span colspan="5">Noch keine Einträge!</span></div>';
            return;
        }
        
        players.forEach((player, index) => {
            const medal = index === 0 ? '🥇' : index === 1 ? '🥈' : index === 2 ? '🥉' : (index + 1) + '.';
            const row = document.createElement('div');
            row.className = 'leaderboard-row' + (index < 3 ? ' top-3' : '');
            row.innerHTML = `
                <span class="medal">${medal}</span>
                <span>${player.username}</span>
                <span>${player.totalScore}</span>
                <span>${player.gamesPlayed}</span>
                <span>${player.accuracy}%</span>
            `;
            body.appendChild(row);
        });
        
    } catch (err) {
        console.log('Leaderboard laden fehlgeschlagen');
    }
}

// === ADMIN ===
async function loadAdminQuestions() {
    try {
        const res = await fetch(API_URL + '/api/admin/questions');
        const questions = await res.json();
        
        const container = document.getElementById('questionsTable');
        container.innerHTML = '';
        
        if (questions.length === 0) {
            container.innerHTML = '<p>Noch keine eigenen Fragen vorhanden.</p>';
            return;
        }
        
        questions.forEach(q => {
            const item = document.createElement('div');
            item.className = 'question-item';
            item.innerHTML = `
                <div class="question-item-header">
                    <div class="question-item-text">${escapeHtml(q.text)}</div>
                    <div class="question-item-actions">
                        <button class="btn btn-secondary" onclick="deleteQuestion(${q.id})">🗑️</button>
                    </div>
                </div>
                <div class="question-item-meta">
                    ✓ ${escapeHtml(q.correctAnswer)} | 
                    Schwierigkeit: ${q.difficulty}
                </div>
            `;
            container.appendChild(item);
        });
        
    } catch (err) {
        console.log('Admin Fragen laden fehlgeschlagen');
    }
}

// Neue Frage erstellen
async function createQuestion() {
    const text = document.getElementById('newQuestionText').value.trim();
    const correctAnswer = document.getElementById('newCorrectAnswer').value.trim();
    const wrongAnswersText = document.getElementById('newWrongAnswers').value.trim();
    const categoryId = parseInt(document.getElementById('newQuestionCategory').value);
    const difficulty = document.getElementById('newQuestionDifficulty').value;
    
    // Validierung
    if (!text) {
        showAdminMessage('Bitte Fragetext eingeben!', true);
        return;
    }
    if (!correctAnswer) {
        showAdminMessage('Bitte richtige Antwort eingeben!', true);
        return;
    }
    if (!wrongAnswersText) {
        showAdminMessage('Bitte falsche Antworten eingeben!', true);
        return;
    }
    
    // Falsche Antworten in Array umwandeln
    const wrongAnswers = wrongAnswersText.split('\n')
        .map(a => a.trim())
        .filter(a => a.length > 0);
    
    if (wrongAnswers.length < 1) {
        showAdminMessage('Mindestens eine falsche Antwort nötig!', true);
        return;
    }
    
    try {
        const res = await fetch(API_URL + '/api/admin/questions', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ text, correctAnswer, wrongAnswers, categoryId, difficulty })
        });
        
        const data = await res.json();
        
        if (data.error) {
            showAdminMessage(data.error, true);
            return;
        }
        
        // Erfolg - Formular leeren
        document.getElementById('newQuestionText').value = '';
        document.getElementById('newCorrectAnswer').value = '';
        document.getElementById('newWrongAnswers').value = '';
        
        showAdminMessage('Frage erfolgreich erstellt! ✓', false);
        loadAdminQuestions();
        
    } catch (err) {
        showAdminMessage('Verbindungsfehler!', true);
    }
}

// Frage löschen
async function deleteQuestion(id) {
    if (!confirm('Frage wirklich löschen?')) return;
    
    try {
        const res = await fetch(API_URL + '/api/admin/questions/' + id, {
            method: 'DELETE'
        });
        
        const data = await res.json();
        
        if (data.success) {
            showAdminMessage('Frage gelöscht!', false);
            loadAdminQuestions();
        } else {
            showAdminMessage(data.error || 'Fehler beim Löschen', true);
        }
        
    } catch (err) {
        showAdminMessage('Verbindungsfehler!', true);
    }
}

function showAdminMessage(msg, isError) {
    const el = document.getElementById('adminMessage');
    el.textContent = msg;
    el.className = isError ? 'error-msg' : 'success-msg';
    
    // Nach 3 Sekunden ausblenden
    setTimeout(() => {
        el.textContent = '';
    }, 3000);
}

// HTML escapen um XSS zu verhindern
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
