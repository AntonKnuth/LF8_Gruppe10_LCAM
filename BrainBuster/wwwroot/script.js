let token = localStorage.getItem('token');
let user = null;
let adminToken = null;
let session = null;

// Initialisierung
document.addEventListener('DOMContentLoaded', function() {
    loadCategories();
    checkAuth();
});

// Blendet den angegebenen View ein und lädt zugehörige Daten
function showView(name) {
    var views = document.querySelectorAll('.view');
    for (var i = 0; i < views.length; i++) {
        views[i].classList.remove('active');
    }
    document.getElementById(name).classList.add('active');
    if (name === 'leaderboard') loadLeaderboard();
    if (name === 'admin') loadAdminQuestions();
}

// Kategorien laden und in beide Select-Elemente einfügen
function loadCategories() {
    fetch('/api/categories')
        .then(function(res) { return res.json(); })
        .then(function(cats) {
            var sel = document.getElementById('category');
            var admin = document.getElementById('newCategory');
            sel.innerHTML = '<option value="0">Alle</option>';
            admin.innerHTML = '<option value="0">Keine</option>';
            for (var i = 0; i < cats.length; i++) {
                sel.innerHTML += '<option value="' + cats[i].id + '">' + cats[i].name + '</option>';
                admin.innerHTML += '<option value="' + cats[i].id + '">' + cats[i].name + '</option>';
            }
        });
}

// Auth-Status mit Server abgleichen
function checkAuth() {
    if (!token) { updateUI(); return; }
    fetch('/api/me', { headers: { Authorization: 'Bearer ' + token } })
        .then(function(res) { return res.json(); })
        .then(function(data) {
            if (data.loggedIn) user = data.player;
            else { token = null; localStorage.removeItem('token'); }
            updateUI();
        });
}

// UI-Elemente je nach Login-Status ein-/ausblenden
function updateUI() {
    var auth = document.getElementById('authBox');
    var player = document.getElementById('playerBox');
    var guest = document.getElementById('guestDiv');
    var info = document.getElementById('userInfo');

    if (user) {
        auth.classList.add('hidden');
        player.classList.remove('hidden');
        guest.classList.add('hidden');
        document.getElementById('playerName').textContent = user.username;
        document.getElementById('playerScore').textContent = user.totalScore;
        info.textContent = user.username;
    } else {
        auth.classList.remove('hidden');
        player.classList.add('hidden');
        guest.classList.remove('hidden');
        info.textContent = '';
    }
}

function login() {
    var u = document.getElementById('loginUser').value;
    var p = document.getElementById('loginPw').value;
    fetch('/api/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: u, password: p })
    })
    .then(function(res) { return res.json(); })
    .then(function(data) {
        if (data.error) { document.getElementById('authMsg').textContent = data.error; return; }
        token = data.token;
        user = data.player;
        localStorage.setItem('token', token);
        updateUI();
    });
}

function register() {
    var u = document.getElementById('loginUser').value;
    var p = document.getElementById('loginPw').value;
    fetch('/api/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: u, password: p })
    })
    .then(function(res) { return res.json(); })
    .then(function(data) {
        if (data.error) { document.getElementById('authMsg').textContent = data.error; return; }
        token = data.token;
        user = data.player;
        localStorage.setItem('token', token);
        updateUI();
    });
}

function logout() {
    token = null;
    user = null;
    localStorage.removeItem('token');
    updateUI();
}

function startGame() {
    var cat = parseInt(document.getElementById('category').value);
    var count = parseInt(document.getElementById('questionCount').value);
    var diff = document.getElementById('difficulty').value;
    var name = user ? user.username : (document.getElementById('guestName').value || 'Gast');

    var headers = { 'Content-Type': 'application/json' };
    if (token) headers.Authorization = 'Bearer ' + token;

    fetch('/api/start', {
        method: 'POST',
        headers: headers,
        body: JSON.stringify({ categoryId: cat, questionCount: count, playerName: name, difficulty: diff })
    })
    .then(function(res) { return res.json(); })
    .then(function(data) {
        if (data.error) { alert(data.error); return; }
        session = data;
        showView('game');
        showQuestion(data.question);
    });
}

function showQuestion(q) {
    if (!q) { showResult(); return; }
    document.getElementById('qNum').textContent = q.number;
    document.getElementById('qTotal').textContent = q.total;
    document.getElementById('score').textContent = session.score;
    document.getElementById('streak').textContent = session.streak;
    document.getElementById('questionText').textContent = q.text;

    var container = document.getElementById('answers');
    container.innerHTML = '';
    for (var i = 0; i < q.answers.length; i++) {
        var btn = document.createElement('button');
        btn.className = 'answer-btn';
        btn.textContent = q.answers[i];
        btn.onclick = (function(answer, button) {
            return function() { selectAnswer(answer, button); };
        })(q.answers[i], btn);
        container.appendChild(btn);
    }
    document.getElementById('feedback').classList.add('hidden');
}

// Antwort senden, Feedback anzeigen und Buttons je nach Ergebnis einfärben
function selectAnswer(answer, btn) {
    var buttons = document.querySelectorAll('.answer-btn');
    for (var i = 0; i < buttons.length; i++) buttons[i].disabled = true;

    fetch('/api/answer', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sessionId: session.sessionId, answer: answer })
    })
    .then(function(res) { return res.json(); })
    .then(function(data) {
        session.score = data.score;
        session.streak = data.streak;
        session.finished = data.finished;
        session.nextQuestion = data.question;
        session.correct = (session.correct || 0) + (data.correct ? 1 : 0);

        var buttons = document.querySelectorAll('.answer-btn');
        for (var i = 0; i < buttons.length; i++) {
            if (buttons[i].textContent === data.correctAnswer) buttons[i].classList.add('correct');
            else if (buttons[i].textContent === answer && !data.correct) buttons[i].classList.add('wrong');
        }

        document.getElementById('feedbackText').textContent = data.correct 
            ? 'Richtig! +' + data.points + ' Punkte' 
            : 'Falsch! Richtig war: ' + data.correctAnswer;
        document.getElementById('feedback').classList.remove('hidden');
    });
}

function nextQuestion() {
    if (session.finished) showResult();
    else showQuestion(session.nextQuestion);
}

function showResult() {
    showView('result');
    document.getElementById('finalScore').textContent = session.score;
    document.getElementById('finalCorrect').textContent = session.correct || 0;
    document.getElementById('finalTotal').textContent = session.total;
    if (user) checkAuth();
}

function loadLeaderboard() {
    fetch('/api/leaderboard')
        .then(function(res) { return res.json(); })
        .then(function(data) {
            var tbody = document.querySelector('#leaderboardTable tbody');
            tbody.innerHTML = '';
            for (var i = 0; i < data.length; i++) {
                var tr = document.createElement('tr');
                var tdRank = document.createElement('td');
                tdRank.textContent = i + 1;
                var tdName = document.createElement('td');
                tdName.textContent = data[i].name;
                var tdScore = document.createElement('td');
                tdScore.textContent = data[i].score;
                tr.appendChild(tdRank);
                tr.appendChild(tdName);
                tr.appendChild(tdScore);
                tbody.appendChild(tr);
            }
        });
}

function adminLogin() {
    var u = document.getElementById('adminUser').value;
    var p = document.getElementById('adminPw').value;
    fetch('/api/admin/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: u, password: p })
    })
    .then(function(res) { return res.json(); })
    .then(function(data) {
        if (data.error) { document.getElementById('adminMsg').textContent = data.error; return; }
        adminToken = data.token;
        document.getElementById('adminLogin').classList.add('hidden');
        document.getElementById('adminPanel').classList.remove('hidden');
        loadAdminQuestions();
    });
}

function loadAdminQuestions() {
    if (!adminToken) return;
    fetch('/api/admin/questions', { headers: { Authorization: 'Bearer ' + adminToken } })
        .then(function(res) { return res.json(); })
        .then(function(data) {
            var list = document.getElementById('questionsList');
            list.innerHTML = '';
            if (data.error) return;
            for (var i = 0; i < data.length; i++) {
                var div = document.createElement('div');
                div.className = 'question-item';
                div.id = 'q-' + data[i].id;

                var viewDiv = document.createElement('div');
                viewDiv.className = 'question-view';

                var b = document.createElement('b');
                b.textContent = data[i].text;
                viewDiv.appendChild(b);
                viewDiv.appendChild(document.createElement('br'));

                var span = document.createTextNode('Antwort: ' + data[i].correctAnswer + ' | Falsch: ' + (data[i].wrongAnswers || []).join(', ') + ' ');
                viewDiv.appendChild(span);

                var editBtn = document.createElement('button');
                editBtn.textContent = 'Bearbeiten';
                editBtn.onclick = (function(q) { return function() { showEditForm(q); }; })(data[i]);
                viewDiv.appendChild(editBtn);

                var delBtn = document.createElement('button');
                delBtn.textContent = 'X';
                delBtn.onclick = (function(id) { return function() { deleteQuestion(id); }; })(data[i].id);
                viewDiv.appendChild(delBtn);

                div.appendChild(viewDiv);
                list.appendChild(div);
            }
        });
}

function showEditForm(q) {
    var div = document.getElementById('q-' + q.id);
    if (!div) return;

    var existing = div.querySelector('.question-edit');
    if (existing) { existing.remove(); return; }

    var form = document.createElement('div');
    form.className = 'question-edit';

    form.innerHTML =
        '<textarea class="edit-text">' + escapeHtml(q.text) + '</textarea>' +
        '<input type="text" class="edit-correct" value="' + escapeHtml(q.correctAnswer) + '" placeholder="Richtige Antwort">' +
        '<textarea class="edit-wrong" placeholder="Falsche Antworten (eine pro Zeile)">' + (q.wrongAnswers || []).join('\n') + '</textarea>' +
        '<select class="edit-category"></select>' +
        '<select class="edit-difficulty">' +
            '<option value="easy"' + (q.difficulty === 'easy' || q.difficulty === 0 ? ' selected' : '') + '>Easy</option>' +
            '<option value="medium"' + (q.difficulty === 'medium' || q.difficulty === 1 ? ' selected' : '') + '>Medium</option>' +
            '<option value="hard"' + (q.difficulty === 'hard' || q.difficulty === 2 ? ' selected' : '') + '>Hard</option>' +
        '</select>' +
        '<button class="edit-save">Speichern</button>' +
        '<button class="edit-cancel">Abbrechen</button>' +
        '<p class="edit-msg"></p>';

    div.appendChild(form);

    // Kategorien laden
    fetch('/api/categories')
        .then(function(res) { return res.json(); })
        .then(function(cats) {
            var sel = form.querySelector('.edit-category');
            sel.innerHTML = '<option value="0">Keine</option>';
            for (var i = 0; i < cats.length; i++) {
                var opt = document.createElement('option');
                opt.value = cats[i].id;
                opt.textContent = cats[i].name;
                if (cats[i].id === q.categoryId) opt.selected = true;
                sel.appendChild(opt);
            }
        });

    form.querySelector('.edit-save').onclick = function() { saveEdit(q.id, form); };
    form.querySelector('.edit-cancel').onclick = function() { form.remove(); };
}

function saveEdit(id, form) {
    var text = form.querySelector('.edit-text').value;
    var correct = form.querySelector('.edit-correct').value;
    var wrong = form.querySelector('.edit-wrong').value.split('\n').filter(function(x) { return x.trim(); });
    var cat = parseInt(form.querySelector('.edit-category').value);
    var diff = form.querySelector('.edit-difficulty').value;

    if (!text || !correct || wrong.length < 1) {
        form.querySelector('.edit-msg').textContent = 'Alle Felder ausfuellen!';
        return;
    }

    fetch('/api/admin/questions/' + id, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + adminToken },
        body: JSON.stringify({ text: text, correctAnswer: correct, wrongAnswers: wrong, categoryId: cat, difficulty: diff })
    })
    .then(function(res) { return res.json(); })
    .then(function(data) {
        if (data.error) { form.querySelector('.edit-msg').textContent = data.error; return; }
        loadAdminQuestions();
    });
}

function escapeHtml(str) {
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function addQuestion() {
    var text = document.getElementById('newQuestion').value;
    var correct = document.getElementById('correctAnswer').value;
    var wrong = document.getElementById('wrongAnswers').value.split('\n').filter(function(x) { return x.trim(); });
    var cat = parseInt(document.getElementById('newCategory').value);
    var diff = document.getElementById('newDifficulty').value;

    if (!text || !correct || wrong.length < 1) {
        document.getElementById('addMsg').textContent = 'Alle Felder ausfuellen!';
        return;
    }

    fetch('/api/admin/questions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + adminToken },
        body: JSON.stringify({ text: text, correctAnswer: correct, wrongAnswers: wrong, categoryId: cat, difficulty: diff })
    })
    .then(function(res) { return res.json(); })
    .then(function(data) {
        if (data.error) { document.getElementById('addMsg').textContent = data.error; return; }
        document.getElementById('addMsg').textContent = 'Gespeichert!';
        document.getElementById('newQuestion').value = '';
        document.getElementById('correctAnswer').value = '';
        document.getElementById('wrongAnswers').value = '';
        loadAdminQuestions();
    });
}

function deleteQuestion(id) {
    if (!confirm('Wirklich loeschen?')) return;
    fetch('/api/admin/questions/' + id, {
        method: 'DELETE',
        headers: { Authorization: 'Bearer ' + adminToken }
    }).then(function() { loadAdminQuestions(); });
}
