// Funções utilitárias para o sistema de torneios

// Inicialização quando o DOM estiver carregado
document.addEventListener('DOMContentLoaded', function () {
    initializeTooltips();
    initializeCountdown();
    initializeFormValidation();
    initializeSearchAndFilter();
});

// Inicializar tooltips do Bootstrap
function initializeTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Contador regressivo para torneios
function initializeCountdown() {
    const countdownElements = document.querySelectorAll('.countdown-timer');

    countdownElements.forEach(element => {
        const targetDate = new Date(element.dataset.targetDate);

        function updateCountdown() {
            const now = new Date().getTime();
            const distance = targetDate - now;

            if (distance < 0) {
                element.innerHTML = "INICIADO";
                element.classList.add('text-danger');
                return;
            }

            const days = Math.floor(distance / (1000 * 60 * 60 * 24));
            const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((distance % (1000 * 60)) / 1000);

            element.innerHTML = `${days}d ${hours}h ${minutes}m ${seconds}s`;
        }

        updateCountdown();
        setInterval(updateCountdown, 1000);
    });
}

// Validação de formulários personalizada
function initializeFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');

    Array.prototype.slice.call(forms).forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });

    // Validação específica para datas do torneio
    const startDateInput = document.querySelector('input[name="StartDate"]');
    const endDateInput = document.querySelector('input[name="EndDate"]');

    if (startDateInput && endDateInput) {
        startDateInput.addEventListener('change', function () {
            validateTournamentDates();
        });

        endDateInput.addEventListener('change', function () {
            validateTournamentDates();
        });
    }
}

// Validar datas do torneio
function validateTournamentDates() {
    const startDateInput = document.querySelector('input[name="StartDate"]');
    const endDateInput = document.querySelector('input[name="EndDate"]');

    if (!startDateInput || !endDateInput) return;

    const startDate = new Date(startDateInput.value);
    const endDate = new Date(endDateInput.value);
    const now = new Date();

    // Data de início deve ser no futuro
    if (startDate <= now) {
        startDateInput.setCustomValidity('A data de início deve ser no futuro');
    } else {
        startDateInput.setCustomValidity('');
    }

    // Data de fim deve ser após data de início
    if (endDate <= startDate) {
        endDateInput.setCustomValidity('A data de fim deve ser após a data de início');
    } else {
        endDateInput.setCustomValidity('');
    }
}

// Sistema de busca e filtros
function initializeSearchAndFilter() {
    const searchInput = document.getElementById('tournament-search');
    const gameFilter = document.getElementById('game-filter');
    const statusFilter = document.getElementById('status-filter');

    if (searchInput) {
        searchInput.addEventListener('input', debounce(filterTournaments, 300));
    }

    if (gameFilter) {
        gameFilter.addEventListener('change', filterTournaments);
    }

    if (statusFilter) {
        statusFilter.addEventListener('change', filterTournaments);
    }
}

// Filtrar torneios
function filterTournaments() {
    const searchTerm = document.getElementById('tournament-search')?.value.toLowerCase() || '';
    const gameFilter = document.getElementById('game-filter')?.value || '';
    const statusFilter = document.getElementById('status-filter')?.value || '';

    const tournamentCards = document.querySelectorAll('.tournament-card');

    tournamentCards.forEach(card => {
        const cardParent = card.closest('.col-lg-4, .col-md-6, .col-lg-6');
        if (!cardParent) return;

        const title = card.querySelector('.card-title')?.textContent.toLowerCase() || '';
        const description = card.querySelector('.card-text')?.textContent.toLowerCase() || '';
        const gameType = card.dataset.game || '';
        const status = card.dataset.status || '';

        const matchesSearch = title.includes(searchTerm) || description.includes(searchTerm);
        const matchesGame = !gameFilter || gameType === gameFilter;
        const matchesStatus = !statusFilter || status === statusFilter;

        if (matchesSearch && matchesGame && matchesStatus) {
            cardParent.style.display = '';
            card.classList.add('fade-in');
        } else {
            cardParent.style.display = 'none';
            card.classList.remove('fade-in');
        }
    });

    updateNoResultsMessage();
}

// Atualizar mensagem de "nenhum resultado"
function updateNoResultsMessage() {
    const visibleCards = document.querySelectorAll('.tournament-card:not([style*="display: none"])');
    const noResultsMsg = document.getElementById('no-results-message');

    if (visibleCards.length === 0 && noResultsMsg) {
        noResultsMsg.style.display = 'block';
    } else if (noResultsMsg) {
        noResultsMsg.style.display = 'none';
    }
}

// Função debounce para otimizar performance
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Confirmação para ações importantes
function confirmAction(message, callback) {
    if (confirm(message)) {
        callback();
    }
}

// Loading state para botões
function setButtonLoading(button, loading = true) {
    if (loading) {
        button.disabled = true;
        button.dataset.originalText = button.innerHTML;
        button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Carregando...';
    } else {
        button.disabled = false;
        button.innerHTML = button.dataset.originalText;
    }
}

// Notificações toast
function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toast-container') || createToastContainer();

    const toastElement = document.createElement('div');
    toastElement.className = `toast align-items-center text-white bg-${type} border-0`;
    toastElement.setAttribute('role', 'alert');
    toastElement.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    toastContainer.appendChild(toastElement);

    const toast = new bootstrap.Toast(toastElement);
    toast.show();

    // Remover após ser fechado
    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
}

// Criar container de toasts se não existir
function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toast-container';
    container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
    container.style.zIndex = '1055';
    document.body.appendChild(container);
    return container;
}

// Atualizar dados automaticamente
function enableAutoRefresh(intervalMs = 30000) {
    setInterval(() => {
        // Atualizar apenas se a página estiver visível
        if (!document.hidden) {
            refreshTournamentData();
        }
    }, intervalMs);
}

// Refresh dos dados de torneio
async function refreshTournamentData() {
    try {
        const currentPath = window.location.pathname;
        if (currentPath.includes('/Tournament/Details/')) {
            const tournamentId = currentPath.split('/').pop();
            await updateTournamentDetails(tournamentId);
        } else if (currentPath.includes('/Tournament')) {
            await updateTournamentList();
        }
    } catch (error) {
        console.error('Erro ao atualizar dados:', error);
    }
}

// Atualizar detalhes do torneio via AJAX
async function updateTournamentDetails(tournamentId) {
    try {
        const response = await fetch(`/Tournament/GetTournamentData/${tournamentId}`);
        if (response.ok) {
            const data = await response.json();
            updateParticipantsList(data.participants);
            updateTournamentStats(data);
        }
    } catch (error) {
        console.error('Erro ao buscar dados do torneio:', error);
    }
}

// Atualizar lista de participantes
function updateParticipantsList(participants) {
    const participantsList = document.querySelector('.participants-list');
    if (!participantsList) return;

    participantsList.innerHTML = '';

    if (participants.length === 0) {
        participantsList.innerHTML = `
            <div class="text-center text-muted py-4">
                <i class="fas fa-user-slash fa-2x mb-2"></i>
                <p>Nenhum participante ainda</p>
            </div>
        `;
        return;
    }

    participants.forEach(participant => {
        const participantElement = document.createElement('div');
        participantElement.className = 'participant-item fade-in';
        participantElement.innerHTML = `
            <div class="participant-info">
                <div class="participant-name">
                    <i class="fas fa-user text-primary me-2"></i>
                    <strong>${participant.playerName}</strong>
                </div>
                <div class="participant-details">
                    <small class="text-muted">
                        <i class="fas fa-gamepad me-1"></i>${participant.gameUsername}
                    </small>
                    ${participant.rank ? `<br><small class="text-info"><i class="fas fa-medal me-1"></i>${participant.rank}</small>` : ''}
                </div>
            </div>
            <small class="text-muted">
                ${new Date(participant.registeredAt).toLocaleDateString('pt-BR', {
            day: '2-digit',
            month: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        })}
            </small>
        `;
        participantsList.appendChild(participantElement);
    });
}

// Atualizar estatísticas do torneio
function updateTournamentStats(data) {
    const participantCount = document.querySelector('.stat-number');
    if (participantCount) {
        participantCount.textContent = data.participants.length;
    }

    const participantHeader = document.querySelector('.card-header h5');
    if (participantHeader) {
        participantHeader.innerHTML = `
            <i class="fas fa-users me-2"></i>
            Participantes (${data.participants.length}/${data.maxParticipants})
        `;
    }
}

// Função para copiar link do torneio
function copyTournamentLink(tournamentId) {
    const url = `${window.location.origin}/Tournament/Details/${tournamentId}`;

    if (navigator.clipboard) {
        navigator.clipboard.writeText(url).then(() => {
            showToast('Link copiado para a área de transferência!', 'success');
        });
    } else {
        // Fallback para navegadores mais antigos
        const textArea = document.createElement('textarea');
        textArea.value = url;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        showToast('Link copiado para a área de transferência!', 'success');
    }
}

// Funções específicas para formulários
document.addEventListener('DOMContentLoaded', function () {
    // Auto-completar nome do jogador com nome do usuário se disponível
    const playerNameInput = document.querySelector('input[name="PlayerName"]');
    const userEmail = document.querySelector('[data-user-email]')?.dataset.userEmail;

    if (playerNameInput && userEmail && !playerNameInput.value) {
        // Extrair nome do email (parte antes do @)
        const userName = userEmail.split('@')[0];
        playerNameInput.placeholder = `Ex: ${userName}`;
    }
});
