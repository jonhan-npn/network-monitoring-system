const host = window.location.hostname;

export const environment = {
  apiUrl: `http://${host}:5000/api`,
  hubUrl: `http://${host}:5000/hubs/monitoring`,
  authenticationEnabled: false,
};
