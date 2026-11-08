const host = window.location.hostname;

export const environment = {
  apiUrl: `http://${host}:5080/api`,
  hubUrl: `http://${host}:5080/hubs/monitoring`,
  authenticationEnabled: true,
};
